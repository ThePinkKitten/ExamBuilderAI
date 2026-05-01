using System.Text.Json;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.Models;
using ExamBuilderAI.API.DTOs.Exercise;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Services;

public class ExerciseGeneratorService
{
    private readonly OpenRouterService _openRouter;
    private readonly AppDbContext _db;
    private readonly ILogger<ExerciseGeneratorService> _logger;

    public ExerciseGeneratorService(OpenRouterService openRouter, AppDbContext db, ILogger<ExerciseGeneratorService> logger)
    {
        _openRouter = openRouter;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Generate an exercise by fetching pre-generated questions from the bank.
    /// Returns null if not enough questions are available (triggers 202).
    /// </summary>
    public async Task<Exercise?> GenerateAsync(string sectionCode, int? curriculumUnitId, int questionCount, int userId)
    {
        var section = await _db.ExerciseSections.FirstOrDefaultAsync(s => s.Code == sectionCode);
        if (section == null) return null;

        var query = _db.Set<Question>().Where(q => q.SectionId == section.Id && !q.IsAssigned);
        
        if (curriculumUnitId.HasValue)
        {
            query = query.Where(q => q.CurriculumUnitId == curriculumUnitId.Value);
        }

        // Randomize order a bit? Or just take the oldest ones. Take oldest ones for now.
        var availableQuestions = await query.OrderBy(q => q.CreatedAt).Take(questionCount).ToListAsync();

        if (availableQuestions.Count < questionCount)
        {
            _logger.LogWarning("Not enough questions in bank for {SectionCode}. Requested {Req}, available {Avail}.", sectionCode, questionCount, availableQuestions.Count);
            return null; 
        }

        foreach (var q in availableQuestions)
        {
            q.IsAssigned = true;
        }

        var exercise = new Exercise
        {
            SectionId = section.Id,
            CurriculumUnitId = curriculumUnitId,
            GeneratedByUserId = userId,
            QuestionCount = questionCount,
            CreatedAt = DateTime.UtcNow
        };

        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync();

        int order = 0;
        foreach (var q in availableQuestions)
        {
            _db.Set<ExerciseQuestion>().Add(new ExerciseQuestion
            {
                ExerciseId = exercise.Id,
                QuestionId = q.Id,
                OrderIndex = order++
            });
        }
        await _db.SaveChangesAsync();

        await _db.Entry(exercise).Reference(e => e.Section).LoadAsync();
        if (exercise.CurriculumUnitId.HasValue)
            await _db.Entry(exercise).Reference(e => e.CurriculumUnit).LoadAsync();
        
        await _db.Entry(exercise).Collection(e => e.ExerciseQuestions)
            .Query().Include(eq => eq.Question).LoadAsync();

        return exercise;
    }

    /// <summary>
    /// Stitches multiple Question entities into a single composite JSON for the frontend.
    /// Re-indexes IDs, concatenates passages, and optionally strips answers.
    /// </summary>
    public static JsonElement BuildCompositeContent(List<Question> questions, string sectionCode, bool stripAnswers)
    {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

        writer.WriteStartObject();

        if (sectionCode == "paragraph_writing")
        {
            var q = questions.FirstOrDefault();
            if (q != null)
            {
                using var doc = JsonDocument.Parse(q.Content);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (stripAnswers && (prop.Name == "sampleAnswer" || prop.Name == "rubric")) continue;
                    prop.WriteTo(writer);
                }
            }
        }
        else if (sectionCode == "reading" || sectionCode == "cloze_test")
        {
            var itemsPropName = sectionCode == "reading" ? "questions" : "blanks";
            var passages = new List<string>();
            
            writer.WritePropertyName(itemsPropName);
            writer.WriteStartArray();
            int newId = 1;
            foreach (var q in questions)
            {
                using var doc = JsonDocument.Parse(q.Content);
                var root = doc.RootElement;
                if (root.TryGetProperty("passage", out var p))
                {
                    passages.Add(p.GetString() ?? "");
                }
                if (root.TryGetProperty(itemsPropName, out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        writer.WriteStartObject();
                        writer.WriteNumber("id", newId++);
                        foreach (var prop in item.EnumerateObject())
                        {
                            if (prop.Name == "id") continue;
                            if (stripAnswers && (prop.Name == "correctAnswer" || prop.Name == "explanation" || prop.Name == "acceptableAnswers")) continue;
                            prop.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    }
                }
            }
            writer.WriteEndArray();
            writer.WriteString("passage", string.Join("\n\n---\n\n", passages));
        }
        else
        {
            writer.WritePropertyName("questions");
            writer.WriteStartArray();
            int newId = 1;
            foreach (var q in questions)
            {
                using var doc = JsonDocument.Parse(q.Content);
                var root = doc.RootElement;
                if (root.TryGetProperty("questions", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        writer.WriteStartObject();
                        writer.WriteNumber("id", newId++);
                        foreach (var prop in item.EnumerateObject())
                        {
                            if (prop.Name == "id") continue;
                            if (stripAnswers && (prop.Name == "correctAnswer" || prop.Name == "explanation" || prop.Name == "acceptableAnswers")) continue;
                            prop.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    }
                }
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.Flush();

        return JsonDocument.Parse(ms.ToArray()).RootElement.Clone();
    }

    public static string GetSystemPrompt()
    {
        return "You are an expert English teacher creating exercises for Vietnamese students. " +
               "Generate content suitable for Grade 8 English level (Global Success textbook). Mix various topics.\n" +
               "Return ONLY valid JSON. Do NOT wrap the JSON in markdown blocks (e.g. ```json). Do NOT add any extra conversational text.\n" +
               "Explanations should be concise and in English (with Vietnamese translation if helpful).\n\n" +
               "CRITICAL RULES FOR EXPLANATIONS:\n" +
               "1. You MUST strictly reference the EXACT options you generated in your explanation.\n" +
               "2. Do NOT hallucinate or include words in the explanation that are not present in the options array.\n" +
               "3. Ensure the correct answer strictly matches the explanation.\n" +
               "4. For pronunciation/stress questions, ensure phonetic accuracy.\n" +
               "5. Each question must have exactly ONE correct answer.";
    }

    public static string BuildPrompt(string sectionCode, CurriculumUnit? unit, int questionCount)
    {
        var unitContext = "";
        if (unit != null)
        {
            unitContext = $@"
CURRICULUM CONTEXT (Unit {unit.UnitNumber}: {unit.UnitTitle}):
- Grammar points: {unit.GrammarPoints}
- Vocabulary: {unit.Vocabulary}
- Topics: {unit.Topics}
Use ONLY the grammar, vocabulary, and topics from this unit.";
        }
        else
        {
            unitContext = "\nGenerate content suitable for Grade 8 English level (Global Success textbook). Mix various topics.";
        }

        return sectionCode switch
        {
            "pronunciation" => $@"Generate {questionCount} pronunciation questions at Grade 8 level.
{unitContext}

Each question: 4 words where 3 share the same pronunciation for an underlined part, and 1 is different.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""instruction"": ""Choose the word whose underlined part is pronounced differently from the others"",
      ""options"": [""word1"", ""word2"", ""word3"", ""word4""],
      ""underlinedParts"": [""ea"", ""ea"", ""ea"", ""ea""],
      ""correctAnswer"": 2,
      ""explanation"": ""Explain why this word is different, include IPA if possible""
    }}
  ]
}}",

            "stress" => $@"Generate {questionCount} stress pattern questions at Grade 8 level.
{unitContext}

Each question: 4 words (2-3 syllables) where 3 share the same stress pattern and 1 is different.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""instruction"": ""Choose the word having a different stress pattern from the others"",
      ""options"": [""word1"", ""word2"", ""word3"", ""word4""],
      ""stressPatterns"": [""Oo"", ""Oo"", ""oO"", ""Oo""],
      ""correctAnswer"": 2,
      ""explanation"": ""Explain the stress difference""
    }}
  ]
}}",

            "grammar_vocab" => $@"Generate {questionCount} grammar and vocabulary MCQ questions at Grade 8 level.
{unitContext}

Each question: a sentence with a blank + 4 options (A, B, C, D).

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""She has been living here ___ 2010."",
      ""options"": [""for"", ""since"", ""during"", ""while""],
      ""correctAnswer"": 1,
      ""explanation"": ""'since' is used with a specific point in time""
    }}
  ]
}}",

            "synonym" => $@"Generate {questionCount} synonym questions at Grade 8 level.
{unitContext}

Each question: a sentence with an underlined word + 4 options. Choose the CLOSEST meaning.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""The movie was absolutely fantastic."",
      ""underlinedWord"": ""fantastic"",
      ""options"": [""terrible"", ""wonderful"", ""boring"", ""normal""],
      ""correctAnswer"": 1,
      ""explanation"": ""'wonderful' is closest in meaning to 'fantastic'""
    }}
  ]
}}",

            "antonym" => $@"Generate {questionCount} antonym questions at Grade 8 level.
{unitContext}

Each question: a sentence with an underlined word + 4 options. Choose the OPPOSITE meaning.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""The exam was extremely difficult."",
      ""underlinedWord"": ""difficult"",
      ""options"": [""hard"", ""easy"", ""challenging"", ""tough""],
      ""correctAnswer"": 1,
      ""explanation"": ""'easy' is the opposite of 'difficult'""
    }}
  ]
}}",

            "cloze_test" => $@"Generate a cloze test passage with {questionCount} blanks at Grade 8 level.
{unitContext}

Create a coherent passage (150-200 words) with numbered blanks. Each blank has 4 options.

Return JSON:
{{
  ""passage"": ""The environment is (1)___ important topic today. Many people (2)___ worried about..."",
  ""blanks"": [
    {{
      ""id"": 1,
      ""options"": [""a"", ""an"", ""the"", ""no article""],
      ""correctAnswer"": 1,
      ""explanation"": ""'an' because 'important' starts with a vowel sound""
    }}
  ]
}}",

            "reading" => $@"Generate a reading comprehension exercise at Grade 8 level.
{unitContext}

Create a passage (200-250 words) with:
- 3 True/False statements
- 2 MCQ questions (4 options each)

Return JSON:
{{
  ""passage"": ""Full passage text here..."",
  ""questions"": [
    {{
      ""id"": 1,
      ""type"": ""true_false"",
      ""statement"": ""Statement about the passage"",
      ""correctAnswer"": true,
      ""explanation"": ""Why this is true/false""
    }},
    {{
      ""id"": 4,
      ""type"": ""mcq"",
      ""question"": ""What is the main idea?"",
      ""options"": [""A. ..."", ""B. ..."", ""C. ..."", ""D. ...""],
      ""correctAnswer"": 1,
      ""explanation"": ""Why this answer is correct""
    }}
  ]
}}",

            "sentence_completion" => $@"Generate {questionCount} sentence completion questions at Grade 8 level.
{unitContext}

Each question: an incomplete sentence that students must complete.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""instruction"": ""Complete the sentence"",
      ""prompt"": ""If it rains tomorrow, we ___"",
      ""correctAnswer"": ""will stay at home"",
      ""acceptableAnswers"": [""will stay at home"", ""won't go out"", ""will not go outside""],
      ""explanation"": ""Conditional type 1: If + present simple, will + V-infinitive""
    }}
  ]
}}",

            "word_form" => $@"Generate {questionCount} word form questions at Grade 8 level.
{unitContext}

Each question: a sentence with a blank + a given base word. Students fill in the correct form.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""She is a very ___ student. (CREATE)"",
      ""givenWord"": ""CREATE"",
      ""correctAnswer"": ""creative"",
      ""acceptableAnswers"": [""creative""],
      ""explanation"": ""We need an adjective to modify 'student'. CREATE → creative""
    }}
  ]
}}",

            "paragraph_writing" => $@"Generate a paragraph writing prompt at Grade 8 level.
{unitContext}

Create a writing topic suitable for Grade 8 students (80-100 words expected).
Include helpful hints and a sample answer for grading reference.

Return JSON:
{{
  ""topic"": ""Write a paragraph (80-100 words) about the benefits of recycling"",
  ""hints"": [""reduce waste"", ""save energy"", ""protect the environment"", ""reuse materials""],
  ""wordCount"": {{ ""min"": 80, ""max"": 100 }},
  ""sampleAnswer"": ""Recycling is one of the most effective ways to protect our environment..."",
  ""rubric"": {{
    ""content"": {{ ""maxScore"": 3, ""description"": ""Relevant ideas, clear topic sentence"" }},
    ""language"": {{ ""maxScore"": 3, ""description"": ""Grammar, vocabulary, spelling accuracy"" }},
    ""organization"": {{ ""maxScore"": 2, ""description"": ""Logical flow, coherence"" }},
    ""mechanics"": {{ ""maxScore"": 2, ""description"": ""Punctuation, capitalization"" }}
  }}
}}",

            _ => throw new ArgumentException($"Unknown section code: {sectionCode}")
        };
    }
}
