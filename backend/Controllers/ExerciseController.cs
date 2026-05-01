using System.Security.Claims;
using System.Text.Json;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.Models;
using ExamBuilderAI.API.DTOs.Exercise;
using ExamBuilderAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExerciseController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ExerciseGeneratorService _generator;
    private readonly ExerciseGradingService _grading;

    public ExerciseController(AppDbContext db, ExerciseGeneratorService generator, ExerciseGradingService grading)
    {
        _db = db;
        _generator = generator;
        _grading = grading;
    }

    /// <summary>
    /// Get all 10 exercise sections with stats for current user.
    /// </summary>
    [HttpGet("sections")]
    public async Task<IActionResult> GetSections()
    {
        var userId = GetUserId();
        var sections = await _db.ExerciseSections
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new SectionInfo
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Icon = s.Icon,
                TotalExercises = s.Exercises.Count(e => e.Results.Any(r => r.UserId == userId)),
                AverageScore = s.Exercises
                    .SelectMany(e => e.Results.Where(r => r.UserId == userId))
                    .Any()
                    ? s.Exercises
                        .SelectMany(e => e.Results.Where(r => r.UserId == userId))
                        .Average(r => r.ScorePercent)
                    : null
            })
            .ToListAsync();

        return Ok(sections);
    }

    /// <summary>
    /// Get all curriculum units.
    /// </summary>
    [HttpGet("units")]
    public async Task<IActionResult> GetUnits()
    {
        var units = await _db.CurriculumUnits
            .OrderBy(u => u.UnitNumber)
            .Select(u => new CurriculumUnitInfo
            {
                Id = u.Id,
                UnitNumber = u.UnitNumber,
                UnitTitle = u.UnitTitle
            })
            .ToListAsync();

        return Ok(units);
    }

    /// <summary>
    /// Generate a new exercise. Returns questions only (answers stripped).
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateExerciseRequest request)
    {
        var userId = GetUserId();

        var exercise = await _generator.GenerateAsync(
            request.SectionCode,
            request.CurriculumUnitId,
            request.QuestionCount,
            userId
        );

        if (exercise == null)
            return StatusCode(202, new { message = "Question bank is being updated. Please try again in a moment.", code = "BANK_UPDATING" });

        var questionsList = exercise.ExerciseQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => eq.Question).ToList();

        var response = new ExerciseResponse
        {
            Id = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            UnitTitle = exercise.CurriculumUnit?.UnitTitle,
            QuestionCount = exercise.QuestionCount,
            Questions = ExerciseGeneratorService.BuildCompositeContent(questionsList, exercise.Section.Code, stripAnswers: true),
            CreatedAt = exercise.CreatedAt,
            HasBeenSubmitted = false
        };

        return Ok(response);
    }

    /// <summary>
    /// Get an exercise by ID. Returns questions only if not yet submitted.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExercise(int id)
    {
        var userId = GetUserId();
        var exercise = await _db.Exercises
            .Include(e => e.Section)
            .Include(e => e.CurriculumUnit)
            .Include(e => e.ExerciseQuestions).ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null) return NotFound();

        var hasSubmitted = await _db.ExerciseResults
            .AnyAsync(r => r.ExerciseId == id && r.UserId == userId);

        var questionsList = exercise.ExerciseQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => eq.Question).ToList();

        var response = new ExerciseResponse
        {
            Id = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            UnitTitle = exercise.CurriculumUnit?.UnitTitle,
            QuestionCount = exercise.QuestionCount,
            Questions = ExerciseGeneratorService.BuildCompositeContent(questionsList, exercise.Section.Code, stripAnswers: !hasSubmitted),
            CreatedAt = exercise.CreatedAt,
            HasBeenSubmitted = hasSubmitted
        };

        return Ok(response);
    }

    /// <summary>
    /// Submit answers for an exercise. Returns graded result with explanations.
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitExerciseRequest request)
    {
        var userId = GetUserId();

        var exercise = await _db.Exercises
            .Include(e => e.Section)
            .Include(e => e.ExerciseQuestions).ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null) return NotFound();

        var result = await _grading.GradeAsync(id, userId, request.UserAnswers, request.TimeTakenSeconds);
        if (result == null)
            return BadRequest(new { message = "Failed to grade exercise." });

        var questionsList = exercise.ExerciseQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => eq.Question).ToList();

        var response = new ExerciseResultResponse
        {
            ExerciseId = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            CorrectCount = result.CorrectCount,
            TotalQuestions = result.TotalQuestions,
            ScorePercent = result.ScorePercent,
            TimeTakenSeconds = result.TimeTakenSeconds,
            FullContent = ExerciseGeneratorService.BuildCompositeContent(questionsList, exercise.Section.Code, stripAnswers: false),
            UserAnswers = JsonDocument.Parse(result.UserAnswers).RootElement.Clone(),
            AiFeedback = result.AiFeedback,
            CompletedAt = result.CompletedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get an exercise's review (already graded result).
    /// </summary>
    [HttpGet("{id}/review")]
    public async Task<IActionResult> GetReview(int id)
    {
        var userId = GetUserId();
        var exercise = await _db.Exercises
            .Include(e => e.Section)
            .Include(e => e.ExerciseQuestions).ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null) return NotFound();

        var result = await _db.ExerciseResults.FirstOrDefaultAsync(r => r.ExerciseId == id && r.UserId == userId);
        if (result == null) return BadRequest(new { message = "Exercise not yet submitted." });

        var questionsList = exercise.ExerciseQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => eq.Question).ToList();

        var response = new ExerciseResultResponse
        {
            ExerciseId = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            CorrectCount = result.CorrectCount,
            TotalQuestions = result.TotalQuestions,
            ScorePercent = result.ScorePercent,
            TimeTakenSeconds = result.TimeTakenSeconds,
            FullContent = ExerciseGeneratorService.BuildCompositeContent(questionsList, exercise.Section.Code, stripAnswers: false),
            UserAnswers = JsonDocument.Parse(result.UserAnswers).RootElement.Clone(),
            AiFeedback = result.AiFeedback,
            CompletedAt = result.CompletedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Re-answer an entire exercise (copies all questions to a new exercise attempt).
    /// </summary>
    [HttpPost("{id}/re-answer")]
    public async Task<IActionResult> ReAnswer(int id)
    {
        var userId = GetUserId();
        var oldExercise = await _db.Exercises
            .Include(e => e.Section)
            .Include(e => e.CurriculumUnit)
            .Include(e => e.ExerciseQuestions)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (oldExercise == null) return NotFound();

        var newExercise = new Exercise
        {
            SectionId = oldExercise.SectionId,
            CurriculumUnitId = oldExercise.CurriculumUnitId,
            GeneratedByUserId = userId,
            QuestionCount = oldExercise.QuestionCount,
            CreatedAt = DateTime.UtcNow
        };

        _db.Exercises.Add(newExercise);
        await _db.SaveChangesAsync();

        foreach (var eq in oldExercise.ExerciseQuestions)
        {
            _db.Set<ExerciseQuestion>().Add(new ExerciseQuestion
            {
                ExerciseId = newExercise.Id,
                QuestionId = eq.QuestionId,
                OrderIndex = eq.OrderIndex
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new { newExerciseId = newExercise.Id });
    }

    /// <summary>
    /// Retake mistakes. Gathers up to 10 wrong questions from past exercises of a specific section, or randomly.
    /// </summary>
    [HttpPost("retake-mistakes")]
    public async Task<IActionResult> RetakeMistakes([FromBody] RetakeMistakesRequest request)
    {
        var userId = GetUserId();
        
        // Find all results
        var resultsQuery = _db.ExerciseResults
            .Include(r => r.Exercise).ThenInclude(e => e.Section)
            .Include(r => r.Exercise).ThenInclude(e => e.ExerciseQuestions).ThenInclude(eq => eq.Question)
            .Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(request.SectionCode))
        {
            resultsQuery = resultsQuery.Where(r => r.Exercise.Section.Code == request.SectionCode);
        }

        var results = await resultsQuery.ToListAsync();
        var wrongQuestionIds = new HashSet<int>();

        foreach (var r in results)
        {
            var exercise = r.Exercise;
            // Paragraph writing doesn't have easily extractable 'wrong questions'
            if (exercise.Section.Code == "paragraph_writing") continue;

            var questionsList = exercise.ExerciseQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => eq.Question).ToList();
            var content = ExerciseGeneratorService.BuildCompositeContent(questionsList, exercise.Section.Code, stripAnswers: false);
            var userAnswers = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(r.UserAnswers);
            if (userAnswers == null) continue;

            var questionsNode = content.TryGetProperty("questions", out var qs) ? qs : 
                               (content.TryGetProperty("blanks", out var bs) ? bs : default);
            
            if (questionsNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var q in questionsNode.EnumerateArray())
                {
                    var qId = q.GetProperty("id").GetInt32().ToString();
                    var newIdIndex = int.Parse(qId) - 1;
                    if (newIdIndex < 0 || newIdIndex >= questionsList.Count) continue;

                    bool isCorrect = false;

                    if (userAnswers.TryGetValue(qId, out var userAnswer))
                    {
                        var type = q.TryGetProperty("type", out var t) ? t.GetString() : null;

                        if (type == "true_false")
                        {
                            var correctAnswer = q.GetProperty("correctAnswer").GetBoolean();
                            if (userAnswer.ValueKind == JsonValueKind.True || userAnswer.ValueKind == JsonValueKind.False)
                            {
                                isCorrect = (userAnswer.GetBoolean() == correctAnswer);
                            }
                        }
                        else if (q.TryGetProperty("correctAnswer", out var correctProp))
                        {
                            if (correctProp.ValueKind == JsonValueKind.Number && userAnswer.ValueKind == JsonValueKind.Number)
                            {
                                isCorrect = (userAnswer.GetInt32() == correctProp.GetInt32());
                            }
                            else if (correctProp.ValueKind == JsonValueKind.String && userAnswer.ValueKind == JsonValueKind.String)
                            {
                                var userText = userAnswer.GetString()?.Trim().ToLowerInvariant() ?? "";
                                var correctText = correctProp.GetString()?.Trim().ToLowerInvariant() ?? "";
                                if (userText == correctText) isCorrect = true;
                                else if (q.TryGetProperty("acceptableAnswers", out var acc))
                                {
                                    foreach (var a in acc.EnumerateArray())
                                    {
                                        if (a.GetString()?.Trim().ToLowerInvariant() == userText) { isCorrect = true; break; }
                                    }
                                }
                            }
                        }
                    }

                    if (!isCorrect)
                    {
                        wrongQuestionIds.Add(questionsList[newIdIndex].Id);
                    }
                }
            }
        }

        if (wrongQuestionIds.Count == 0)
            return BadRequest(new { message = "You have no mistakes to fix!" });

        // Grab up to QuestionCount wrong questions
        var randomWrongIds = wrongQuestionIds.OrderBy(x => Guid.NewGuid()).Take(request.QuestionCount ?? 10).ToList();

        // Need to create a new exercise. We need a section. 
        // If they requested a specific section, use that.
        // Otherwise, group by section and pick the most common one, or just pick the section of the first question.
        var firstQ = await _db.Set<Question>().Include(q => q.Section).FirstOrDefaultAsync(q => q.Id == randomWrongIds.First());
        if (firstQ == null) return BadRequest(new { message = "Error finding question." });

        var newExercise = new Exercise
        {
            SectionId = firstQ.SectionId, // Note: mixing questions from different sections in one exercise breaks the player. So we enforce single section.
            GeneratedByUserId = userId,
            QuestionCount = randomWrongIds.Count,
            CreatedAt = DateTime.UtcNow
        };

        _db.Exercises.Add(newExercise);
        await _db.SaveChangesAsync();

        int order = 0;
        foreach (var qid in randomWrongIds)
        {
            _db.Set<ExerciseQuestion>().Add(new ExerciseQuestion
            {
                ExerciseId = newExercise.Id,
                QuestionId = qid,
                OrderIndex = order++
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new { newExerciseId = newExercise.Id });
    }

    /// <summary>
    /// Get exercise history for current user.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();

        var query = _db.Exercises
            .Include(e => e.Section)
            .Include(e => e.CurriculumUnit)
            .Where(e => e.GeneratedByUserId == userId || e.Results.Any(r => r.UserId == userId))
            .OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExerciseHistoryItem
            {
                ExerciseId = e.Id,
                SectionCode = e.Section.Code,
                SectionName = e.Section.Name,
                UnitTitle = e.CurriculumUnit != null ? e.CurriculumUnit.UnitTitle : null,
                ScorePercent = e.Results
                    .Where(r => r.UserId == userId)
                    .Select(r => (double?)r.ScorePercent)
                    .FirstOrDefault(),
                CreatedAt = e.CreatedAt,
                CompletedAt = e.Results
                    .Where(r => r.UserId == userId)
                    .Select(r => (DateTime?)r.CompletedAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim ?? "0");
    }
}
