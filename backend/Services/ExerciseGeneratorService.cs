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
            "pronunciation" => $@"Generate {questionCount} pronunciation questions for Grade 8 English students.
Context: {unitContext}

Requirements:
- Each question must contain exactly 4 words.
- Exactly 3 words must share the SAME pronunciation for a specific underlined part, and EXACTLY 1 word must be DIFFERENT.
- Vary the phonetic targets across questions (e.g., vowels, consonants, diphthongs, silent letters).
- The `correctAnswer` must be an integer representing the 0-based index of the correct option.
- In the explanation, you MUST include the IPA transcriptions for the underlined parts of all 4 options to clearly justify the answer.
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""instruction"": ""Choose the word whose underlined part is pronounced differently from the others"",
      ""options"": [""machine"", ""champion"", ""children"", ""teacher""],
      ""underlinedParts"": [""ch"", ""ch"", ""ch"", ""ch""],
      ""correctAnswer"": 0,
      ""explanation"": ""The 'ch' in 'machine' is pronounced as /ʃ/, while in the others it is pronounced as /tʃ/.""
    }}
  ]
}}",

            "ed_pronunciation" => $@"Generate {questionCount} pronunciation questions focusing on '-ed' endings for Grade 8 English students.
Context: {unitContext}

Requirements:
- Each question must contain exactly 4 common regular verbs in past tense (-ed).
- Exactly 3 words must have the SAME '-ed' pronunciation, and EXACTLY 1 word must be DIFFERENT.
- The underlined part must always be ""ed"" for all options.
- Use only standard '-ed' pronunciation rules: /t/, /d/, /ɪd/.
- Randomize verbs and pronunciation patterns across questions.
- The `correctAnswer` must be an integer representing the 0-based index of the correct option.
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""instruction"": ""Choose the word whose -ed part is pronounced differently from the others"",
      ""options"": [""worked"", ""watched"", ""wanted"", ""stopped""],
      ""underlinedParts"": [""ed"", ""ed"", ""ed"", ""ed""],
      ""correctAnswer"": 2,
      ""explanation"": ""'wanted' is pronounced /ˈwɒntɪd/ with /ɪd/ because the base verb ends in 't', while the others are pronounced /t/.""
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
      ""underlinedParts"": [""o"", ""o"", ""o"", ""o""],
      ""stressPatterns"": [""Oo"", ""Oo"", ""oO"", ""Oo""],
      ""correctAnswer"": 2,
      ""explanation"": ""Explain the stress difference""
    }}
  ]
}}
IMPORTANT: Even for stress pattern, you MUST provide 'underlinedParts' (usually the main vowel of the first or second syllable) to maintain visual consistency with pronunciation questions.",

            "grammar_vocab" => $@"Generate {questionCount} grammar and vocabulary multiple-choice questions for Grade 8 English students.
Context: {unitContext}

Requirements:
- Each question must be a sentence with a single blank space represented by '___'.
- Provide exactly 4 options. The options MUST NOT contain prefixes like 'A.', 'B.', 'C.', 'D.'.
- Balance the questions between grammar rules and vocabulary usage based on the context.
- The `correctAnswer` must be an integer representing the 0-based index of the correct option.
- Explain the grammar rule or vocabulary usage clearly in the explanation.
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""She has been living here ___ 2010."",
      ""options"": [""for"", ""since"", ""during"", ""while""],
      ""correctAnswer"": 1,
      ""explanation"": ""'Since' is used with the present perfect tense to indicate a specific starting point in time (2010), whereas 'for' is used for a duration of time.""
    }}
  ]
}}",

            "synonym" => $@"Generate {questionCount} synonym multiple-choice questions for Grade 8 English students.
Context: {unitContext}

Requirements:
- Each question must consist of a sentence with one specific word intended to be underlined.
- Provide exactly 4 options. The options MUST NOT contain prefixes like 'A.', 'B.', 'C.', 'D.'.
- The objective is to choose the option that has the CLOSEST meaning to the target word in the given context.
- The `underlinedWord` field MUST match the exact spelling and casing of the target word as it appears in the `sentence` (excluding punctuation) to allow exact string matching on the frontend.
- The `correctAnswer` must be an integer representing the 0-based index of the correct option.
- Explain the meaning of the target word and the correct synonym clearly in the explanation.
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""The special effects in the movie were absolutely fantastic."",
      ""underlinedWord"": ""fantastic"",
      ""options"": [""terrible"", ""wonderful"", ""boring"", ""normal""],
      ""correctAnswer"": 1,
      ""explanation"": ""In this context, 'fantastic' describes something exceptionally good or great. 'Wonderful' is the closest in meaning, while the other options have negative or neutral meanings.""
    }}
  ]
}}",

            "antonym" => $@"Generate {questionCount} antonym multiple-choice questions for Grade 8 English students.
Context: {unitContext}

Requirements:
- Each question must consist of a sentence with one specific word intended to be underlined.
- Provide exactly 4 options. The options MUST NOT contain prefixes like 'A.', 'B.', 'C.', 'D.'.
- The objective is to choose the option that has the OPPOSITE meaning to the target word in the given context.
- The `underlinedWord` field MUST match the exact spelling and casing of the target word as it appears in the `sentence` (excluding punctuation) to allow exact string matching on the frontend.
- The `correctAnswer` must be an integer representing the 0-based index of the correct option.
- Explain the meaning of the target word and the correct antonym clearly in the explanation.
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""The movie was extremely boring."",
      ""underlinedWord"": ""boring"",
      ""options"": [""interesting"", ""dull"", ""tiresome"", ""tedious""],
      ""correctAnswer"": 0,
      ""explanation"": ""'Interesting' is the opposite of 'boring'.""
    }}
  ]
}}",

            "cloze_test" => $@"Generate a coherent cloze test passage for Grade 8 English students.
Context: {unitContext}

Requirements:
- The passage should be between 150-200 words.
- Create exactly {questionCount} blanks in the passage, represented as '(1)___', '(2)___', etc.
- Each blank must have exactly 4 options. The options MUST NOT contain prefixes like 'A.', 'B.', 'C.', 'D.'.
- The `blanks` array must contain {questionCount} objects, where the `id` matches the number in the passage.
- The `correctAnswer` must be an integer representing the 0-based index of the correct option.
- Ensure the questions cover a mix of grammar (articles, prepositions, tenses) and vocabulary context.
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""passage"": ""The environment is (1)___ important topic today. Many people (2)___ worried about climate change and its effects on our planet."",
  ""blanks"": [
    {{
      ""id"": 1,
      ""options"": [""a"", ""an"", ""the"", ""no article""],
      ""correctAnswer"": 1,
      ""explanation"": ""We use 'an' because the following word 'important' begins with a vowel sound.""
    }},
    {{
      ""id"": 2,
      ""options"": [""is"", ""am"", ""are"", ""be""],
      ""correctAnswer"": 2,
      ""explanation"": ""'People' is a plural noun, so we use the plural verb 'are'.""
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

            "sentence_completion" => $@"Generate {questionCount} sentence completion questions for Grade 8 English students.
Context: {unitContext}

Requirements:
- Each question must provide an incomplete sentence (the `prompt`) that requires a logical and grammatically correct completion.
- The `correctAnswer` should be the most common/natural completion.
- The `acceptableAnswers` array MUST include at least 3-4 other grammatically correct variations to help with flexible grading.
- Completions should ideally be between 3 to 7 words long.
- The questions should focus on Grade 8 structures (e.g., Conditionals, Relative Clauses, Reported Speech, or Tenses).
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""instruction"": ""Complete the following sentence with a suitable phrase"",
      ""prompt"": ""If it rains tomorrow, we ___"",
      ""correctAnswer"": ""will stay at home"",
      ""acceptableAnswers"": [""will stay at home"", ""won't go out"", ""will not go outside"", ""will stay inside""],
      ""explanation"": ""This is a Conditional Sentence Type 1, used to talk about a possible future event. Structure: If + Present Simple, Will + Verb (bare).""
    }}
  ]
}}",

            "word_form" => $@"Generate {questionCount} word form (word formation) questions for Grade 8 English students.
Context: {unitContext}

Requirements:
- Each question must provide a sentence with a single blank space '___' and a base word in parentheses at the end of the sentence.
- The `givenWord` must be the base form (root) provided in the parentheses (e.g., CREATE, BEAUTY, NATION).
- The `correctAnswer` must be the grammatically correct form of the `givenWord` to fit the sentence.
- The `acceptableAnswers` array should include the correct form, and pay close attention to plural forms or specific verb tenses if the context requires them.
- Explain the grammatical reason for the transformation (e.g., changing a verb to an adjective to modify a noun).
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""questions"": [
    {{
      ""id"": 1,
      ""sentence"": ""She is a very ___ student. (CREATE)"",
      ""givenWord"": ""CREATE"",
      ""correctAnswer"": ""creative"",
      ""acceptableAnswers"": [""creative""],
      ""explanation"": ""In this sentence, an adjective is needed to modify the noun 'student'. The adjective form of the verb 'create' is 'creative'.""
    }}
  ]
}}",

            "paragraph_writing" => $@"Generate a paragraph writing prompt for Grade 8 English students.
Context: {unitContext}

Requirements:
- Create a writing topic that is relevant to the unit context and suitable for the 14-year-old level.
- The `topic` should clearly state the required word count (80-100 words).
- The `hints` array should provide 4-5 helpful ideas or key vocabulary/phrases to guide the student.
- The `sampleAnswer` MUST be a high-quality paragraph that strictly adheres to the 80-100 word limit to serve as a perfect reference.
- Maintain the provided `rubric` structure for consistent grading.
- Output ONLY raw, valid JSON. Do not include markdown code blocks, tags, or any conversational text.

Return JSON:
{{
  ""topic"": ""Write a paragraph (80-100 words) about the benefits of recycling."",
  ""hints"": [""reduce waste"", ""save energy"", ""protect the environment"", ""reuse materials""],
  ""wordCount"": {{ ""min"": 80, ""max"": 100 }},
  ""sampleAnswer"": ""Recycling is one of the most effective ways to protect our environment. First, it helps to reduce the amount of waste sent to landfills. Second, recycling saves energy because manufacturing products from recycled materials uses less power than using raw resources. Finally, it helps protect natural habitats by decreasing the need for mining and logging. In conclusion, every small effort in recycling contributes to a greener and cleaner planet for our future generations."",
  ""rubric"": {{
    ""content"": {{ ""maxScore"": 3, ""description"": ""Relevant ideas, clear topic sentence"" }},
    ""language"": {{ ""maxScore"": 3, ""description"": ""Grammar, vocabulary, spelling accuracy"" }},
    ""organization"": {{ ""maxScore"": 2, ""description"": ""Logical flow, use of connecting words"" }},
    ""mechanics"": {{ ""maxScore"": 2, ""description"": ""Punctuation, capitalization"" }}
  }}
}}",

            _ => throw new ArgumentException($"Unknown section code: {sectionCode}")
        };
    }
}
