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
    /// Generate an exercise for the given section. Returns the saved Exercise entity.
    /// </summary>
    public async Task<Exercise?> GenerateAsync(string sectionCode, int? curriculumUnitId, int questionCount, string difficulty, int userId)
    {
        var section = await _db.ExerciseSections.FirstOrDefaultAsync(s => s.Code == sectionCode);
        if (section == null) return null;

        // Load curriculum unit if specified
        CurriculumUnit? unit = null;
        if (curriculumUnitId.HasValue)
        {
            unit = await _db.CurriculumUnits.FindAsync(curriculumUnitId.Value);
        }

        // Build the prompt
        var systemPrompt = GetSystemPrompt();
        var userPrompt = BuildPrompt(sectionCode, unit, questionCount, difficulty);

        _logger.LogInformation("Generating exercise: section={Section}, unit={Unit}, count={Count}, difficulty={Difficulty}",
            sectionCode, unit?.UnitTitle ?? "general", questionCount, difficulty);

        // Call AI
        var jsonDoc = await _openRouter.ChatCompletionJsonAsync(systemPrompt, userPrompt);
        if (jsonDoc == null)
        {
            _logger.LogError("AI returned null for exercise generation");
            return null;
        }

        // Validate and save
        var content = jsonDoc.RootElement.GetRawText();

        var exercise = new Exercise
        {
            SectionId = section.Id,
            CurriculumUnitId = curriculumUnitId,
            GeneratedByUserId = userId,
            Difficulty = difficulty,
            QuestionCount = questionCount,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(exercise).Reference(e => e.Section).LoadAsync();
        if (exercise.CurriculumUnitId.HasValue)
            await _db.Entry(exercise).Reference(e => e.CurriculumUnit).LoadAsync();

        return exercise;
    }

    /// <summary>
    /// Generate an exercise via stream. Yields JSON serialized events (chunk, done, error).
    /// </summary>
    public async IAsyncEnumerable<string> GenerateStreamAsync(string sectionCode, int? curriculumUnitId, int questionCount, string difficulty, int userId)
    {
        var section = await _db.ExerciseSections.FirstOrDefaultAsync(s => s.Code == sectionCode);
        if (section == null)
        {
            yield return JsonSerializer.Serialize(new { type = "error", message = "Invalid section" });
            yield break;
        }

        // Load curriculum unit if specified
        CurriculumUnit? unit = null;
        if (curriculumUnitId.HasValue)
        {
            unit = await _db.CurriculumUnits.FindAsync(curriculumUnitId.Value);
        }

        // Build the prompt
        var systemPrompt = GetSystemPrompt();
        var userPrompt = BuildPrompt(sectionCode, unit, questionCount, difficulty);

        _logger.LogInformation("Generating exercise (stream): section={Section}, unit={Unit}, count={Count}, difficulty={Difficulty}",
            sectionCode, unit?.UnitTitle ?? "general", questionCount, difficulty);

        var fullContentBuilder = new System.Text.StringBuilder();

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Consume the stream
        await foreach (var token in _openRouter.ChatCompletionStreamAsync(systemPrompt, userPrompt))
        {
            fullContentBuilder.Append(token);
            yield return JsonSerializer.Serialize(new { type = "chunk", text = token }, jsonOptions);
        }

        var fullContent = fullContentBuilder.ToString();
        
        // Strip markdown if any (AI might sometimes wrap in ```json)
        fullContent = fullContent.Trim();
        if (fullContent.StartsWith("```json")) fullContent = fullContent.Substring(7);
        else if (fullContent.StartsWith("```")) fullContent = fullContent.Substring(3);
        if (fullContent.EndsWith("```")) fullContent = fullContent.Substring(0, fullContent.Length - 3);
        fullContent = fullContent.Trim();

        // Validate JSON
        bool parseSuccess = true;
        try
        {
            JsonDocument.Parse(fullContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse final streamed JSON: {Content}", fullContent);
            parseSuccess = false;
        }

        if (!parseSuccess)
        {
            yield return JsonSerializer.Serialize(new { type = "error", message = "Failed to parse AI output." }, jsonOptions);
            yield break;
        }

        // Save to DB
        var exercise = new Exercise
        {
            SectionId = section.Id,
            CurriculumUnitId = curriculumUnitId,
            GeneratedByUserId = userId,
            Difficulty = difficulty,
            QuestionCount = questionCount,
            Content = fullContent,
            CreatedAt = DateTime.UtcNow
        };

        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(exercise).Reference(e => e.Section).LoadAsync();
        if (exercise.CurriculumUnitId.HasValue)
            await _db.Entry(exercise).Reference(e => e.CurriculumUnit).LoadAsync();

        // Yield final done event
        var response = new ExerciseResponse
        {
            Id = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            UnitTitle = exercise.CurriculumUnit?.UnitTitle,
            Difficulty = exercise.Difficulty,
            QuestionCount = exercise.QuestionCount,
            Questions = StripAnswers(exercise.Content, exercise.Section.Code),
            CreatedAt = exercise.CreatedAt,
            HasBeenSubmitted = false
        };

        yield return JsonSerializer.Serialize(new { type = "done", exercise = response }, jsonOptions);
    }

    /// <summary>
    /// Extract questions-only content from full exercise content (strip answers + explanations).
    /// </summary>
    public static JsonElement StripAnswers(string content, string sectionCode)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            if (sectionCode == "cloze_test")
                StripClozeAnswers(root, writer);
            else if (sectionCode == "reading")
                StripReadingAnswers(root, writer);
            else if (sectionCode == "paragraph_writing")
                StripWritingAnswers(root, writer);
            else
                StripMcqOrFillAnswers(root, writer);
        }

        return JsonDocument.Parse(ms.ToArray()).RootElement.Clone();
    }

    private static void StripMcqOrFillAnswers(JsonElement root, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (root.TryGetProperty("questions", out var questions))
        {
            writer.WriteStartArray("questions");
            foreach (var q in questions.EnumerateArray())
            {
                writer.WriteStartObject();
                foreach (var prop in q.EnumerateObject())
                {
                    if (prop.Name != "correctAnswer" && prop.Name != "explanation" && prop.Name != "acceptableAnswers")
                        prop.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    private static void StripClozeAnswers(JsonElement root, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (root.TryGetProperty("passage", out var passage))
        {
            writer.WriteString("passage", passage.GetString());
        }
        if (root.TryGetProperty("blanks", out var blanks))
        {
            writer.WriteStartArray("blanks");
            foreach (var b in blanks.EnumerateArray())
            {
                writer.WriteStartObject();
                foreach (var prop in b.EnumerateObject())
                {
                    if (prop.Name != "correctAnswer" && prop.Name != "explanation")
                        prop.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    private static void StripReadingAnswers(JsonElement root, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (root.TryGetProperty("passage", out var passage))
        {
            writer.WriteString("passage", passage.GetString());
        }
        if (root.TryGetProperty("questions", out var questions))
        {
            writer.WriteStartArray("questions");
            foreach (var q in questions.EnumerateArray())
            {
                writer.WriteStartObject();
                foreach (var prop in q.EnumerateObject())
                {
                    if (prop.Name != "correctAnswer" && prop.Name != "explanation")
                        prop.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    private static void StripWritingAnswers(JsonElement root, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (root.TryGetProperty("topic", out var topic))
            writer.WriteString("topic", topic.GetString());
        if (root.TryGetProperty("hints", out var hints))
        {
            writer.WritePropertyName("hints");
            hints.WriteTo(writer);
        }
        if (root.TryGetProperty("wordCount", out var wc))
        {
            writer.WritePropertyName("wordCount");
            wc.WriteTo(writer);
        }
        // Strip sampleAnswer and rubric
        writer.WriteEndObject();
    }

    private static string GetSystemPrompt()
    {
        return @"You are an expert English teacher specialized in Vietnamese Grade 8 English curriculum (Global Success textbook).
Your task is to generate exam exercises in STRICT JSON format.

RULES:
1. All content must be appropriate for Vietnamese Grade 8 students (age 13-14).
2. Return ONLY valid JSON — no markdown, no explanations outside JSON.
3. Explanations should be concise and in English (with Vietnamese translation if helpful).
4. For pronunciation/stress questions, ensure phonetic accuracy.
5. Each question must have exactly ONE correct answer.
6. Difficulty levels: easy (basic vocab/grammar), medium (mixed), hard (challenging but still grade 8 level).";
    }

    private static string BuildPrompt(string sectionCode, CurriculumUnit? unit, int questionCount, string difficulty)
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
            "pronunciation" => $@"Generate {questionCount} pronunciation questions at {difficulty} difficulty.
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

            "stress" => $@"Generate {questionCount} stress pattern questions at {difficulty} difficulty.
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

            "grammar_vocab" => $@"Generate {questionCount} grammar and vocabulary MCQ questions at {difficulty} difficulty.
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

            "synonym" => $@"Generate {questionCount} synonym questions at {difficulty} difficulty.
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

            "antonym" => $@"Generate {questionCount} antonym questions at {difficulty} difficulty.
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

            "cloze_test" => $@"Generate a cloze test passage with {questionCount} blanks at {difficulty} difficulty.
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

            "reading" => $@"Generate a reading comprehension exercise at {difficulty} difficulty.
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

            "sentence_completion" => $@"Generate {questionCount} sentence completion questions at {difficulty} difficulty.
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

            "word_form" => $@"Generate {questionCount} word form questions at {difficulty} difficulty.
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

            "paragraph_writing" => $@"Generate a paragraph writing prompt at {difficulty} difficulty.
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
