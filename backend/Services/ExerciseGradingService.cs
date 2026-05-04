using System.Text.Json;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Services;

public class ExerciseGradingService
{
    private readonly AppDbContext _db;
    private readonly OpenRouterService _openRouter;
    private readonly GeminiService _gemini;
    private readonly ILogger<ExerciseGradingService> _logger;
    private readonly IConfiguration _config;

    public ExerciseGradingService(AppDbContext db, OpenRouterService openRouter, GeminiService gemini, ILogger<ExerciseGradingService> logger, IConfiguration config)
    {
        _db = db;
        _openRouter = openRouter;
        _gemini = gemini;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Grade user's submission against stored answers. Returns the ExerciseResult.
    /// </summary>
    public async Task<ExerciseResult?> GradeAsync(int exerciseId, int userId, Dictionary<string, JsonElement> userAnswers, int timeTakenSeconds)
    {
        var exercise = await _db.Exercises
            .Include(e => e.Section)
            .Include(e => e.ExerciseQuestions).ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null) return null;

        // Check if already submitted by this user
        var existing = await _db.ExerciseResults
            .FirstOrDefaultAsync(r => r.ExerciseId == exerciseId && r.UserId == userId);
        if (existing != null) return existing; // Already graded

        var sectionCode = exercise.Section.Code;
        int correctCount = 0;
        int totalQuestions = exercise.QuestionCount;
        string? aiFeedback = null;

        var questionsList = exercise.ExerciseQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => eq.Question).ToList();
        var content = ExerciseGeneratorService.BuildCompositeContent(questionsList, sectionCode, stripAnswers: false);

        if (sectionCode == "paragraph_writing")
        {
            // Writing needs AI grading
            var (score, feedback) = await GradeWritingAsync(content, userAnswers);
            correctCount = score;
            totalQuestions = 10; // Total rubric score
            aiFeedback = feedback;
        }
        else if (sectionCode == "cloze_test")
        {
            correctCount = GradeCloze(content, userAnswers);
            totalQuestions = content.GetProperty("blanks").GetArrayLength();
        }
        else if (sectionCode == "reading")
        {
            correctCount = GradeReading(content, userAnswers);
            totalQuestions = content.GetProperty("questions").GetArrayLength();
        }
        else if (sectionCode == "sentence_completion" || sectionCode == "word_form")
        {
            correctCount = GradeFillIn(content, userAnswers);
            totalQuestions = content.GetProperty("questions").GetArrayLength();
        }
        else
        {
            // MCQ sections: pronunciation, stress, grammar_vocab, synonym, antonym
            correctCount = GradeMcq(content, userAnswers);
            totalQuestions = content.GetProperty("questions").GetArrayLength();
        }

        var scorePercent = totalQuestions > 0 ? Math.Round((double)correctCount / totalQuestions * 100, 1) : 0;

        var result = new ExerciseResult
        {
            UserId = userId,
            ExerciseId = exerciseId,
            UserAnswers = JsonSerializer.Serialize(userAnswers),
            CorrectCount = correctCount,
            TotalQuestions = totalQuestions,
            ScorePercent = scorePercent,
            TimeTakenSeconds = timeTakenSeconds,
            AiFeedback = aiFeedback,
            CompletedAt = DateTime.UtcNow
        };

        _db.ExerciseResults.Add(result);
        await _db.SaveChangesAsync();

        return result;
    }

    private static int GradeMcq(JsonElement content, Dictionary<string, JsonElement> userAnswers)
    {
        int correct = 0;
        var questions = content.GetProperty("questions");

        foreach (var q in questions.EnumerateArray())
        {
            var qId = q.GetProperty("id").GetInt32().ToString();
            var correctAnswer = q.GetProperty("correctAnswer").GetInt32();

            if (userAnswers.TryGetValue(qId, out var userAnswer))
            {
                if (userAnswer.ValueKind == JsonValueKind.Number && userAnswer.GetInt32() == correctAnswer)
                    correct++;
            }
        }

        return correct;
    }

    private static int GradeCloze(JsonElement content, Dictionary<string, JsonElement> userAnswers)
    {
        int correct = 0;
        var blanks = content.GetProperty("blanks");

        foreach (var b in blanks.EnumerateArray())
        {
            var bId = b.GetProperty("id").GetInt32().ToString();
            var correctAnswer = b.GetProperty("correctAnswer").GetInt32();

            if (userAnswers.TryGetValue(bId, out var userAnswer))
            {
                if (userAnswer.ValueKind == JsonValueKind.Number && userAnswer.GetInt32() == correctAnswer)
                    correct++;
            }
        }

        return correct;
    }

    private static int GradeReading(JsonElement content, Dictionary<string, JsonElement> userAnswers)
    {
        int correct = 0;
        var questions = content.GetProperty("questions");

        foreach (var q in questions.EnumerateArray())
        {
            var qId = q.GetProperty("id").GetInt32().ToString();
            var type = q.GetProperty("type").GetString();

            if (!userAnswers.TryGetValue(qId, out var userAnswer)) continue;

            if (type == "true_false")
            {
                var correctAnswer = q.GetProperty("correctAnswer").GetBoolean();
                if (userAnswer.ValueKind == JsonValueKind.True || userAnswer.ValueKind == JsonValueKind.False)
                {
                    if (userAnswer.GetBoolean() == correctAnswer)
                        correct++;
                }
            }
            else // mcq
            {
                var correctAnswer = q.GetProperty("correctAnswer").GetInt32();
                if (userAnswer.ValueKind == JsonValueKind.Number && userAnswer.GetInt32() == correctAnswer)
                    correct++;
            }
        }

        return correct;
    }

    private static int GradeFillIn(JsonElement content, Dictionary<string, JsonElement> userAnswers)
    {
        int correct = 0;
        var questions = content.GetProperty("questions");

        foreach (var q in questions.EnumerateArray())
        {
            var qId = q.GetProperty("id").GetInt32().ToString();

            if (!userAnswers.TryGetValue(qId, out var userAnswer)) continue;
            if (userAnswer.ValueKind != JsonValueKind.String) continue;

            var userText = userAnswer.GetString()?.Trim().ToLowerInvariant() ?? "";

            // Check against correctAnswer and acceptableAnswers
            var correctAnswer = q.GetProperty("correctAnswer").GetString()?.Trim().ToLowerInvariant() ?? "";

            if (userText == correctAnswer)
            {
                correct++;
                continue;
            }

            // Check acceptable answers
            if (q.TryGetProperty("acceptableAnswers", out var acceptable))
            {
                foreach (var alt in acceptable.EnumerateArray())
                {
                    if (alt.GetString()?.Trim().ToLowerInvariant() == userText)
                    {
                        correct++;
                        break;
                    }
                }
            }
        }

        return correct;
    }

    private async Task<(int score, string feedback)> GradeWritingAsync(JsonElement content, Dictionary<string, JsonElement> userAnswers)
    {
        if (!userAnswers.TryGetValue("1", out var writingAnswer) || writingAnswer.ValueKind != JsonValueKind.String)
            return (0, "No writing submitted.");

        var studentText = writingAnswer.GetString() ?? "";
        var topic = content.GetProperty("topic").GetString() ?? "";
        var sampleAnswer = content.TryGetProperty("sampleAnswer", out var sa) ? sa.GetString() : "";

        var prompt = $@"Grade this student's paragraph writing (Grade 8 level).

TOPIC: {topic}
SAMPLE ANSWER (for reference): {sampleAnswer}

STUDENT'S ANSWER:
{studentText}

Grade using this rubric (total 10 points):
- Content (0-3): Relevant ideas, clear topic sentence
- Language (0-3): Grammar, vocabulary, spelling accuracy
- Organization (0-2): Logical flow, coherence
- Mechanics (0-2): Punctuation, capitalization

Return JSON:
{{
  ""content_score"": 0,
  ""language_score"": 0,
  ""organization_score"": 0,
  ""mechanics_score"": 0,
  ""total_score"": 0,
  ""feedback"": ""Detailed feedback for the student with specific suggestions for improvement""
}}";

        var provider = _config["AIProvider"] ?? "OpenRouter";
        JsonDocument? result = null;

        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            result = await _gemini.GenerateContentAsync(
                "You are an English teacher grading Grade 8 student writing. Be encouraging but honest. Provide specific feedback.",
                prompt
            );
        }
        else
        {
            result = await _openRouter.ChatCompletionJsonAsync(
                "You are an English teacher grading Grade 8 student writing. Be encouraging but honest. Provide specific feedback.",
                prompt
            );
        }

        if (result == null)
            return (5, "Unable to grade automatically. Please ask your teacher to review.");

        try
        {
            var root = result.RootElement;
            var totalScore = root.GetProperty("total_score").GetInt32();
            var feedback = root.GetProperty("feedback").GetString() ?? "No feedback available.";
            return (totalScore, feedback);
        }
        catch
        {
            return (5, "Error parsing grade. Please try again.");
        }
    }
}
