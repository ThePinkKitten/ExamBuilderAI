using System.Security.Claims;
using System.Text.Json;
using ExamBuilderAI.API.Data;
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
            request.Difficulty,
            userId
        );

        if (exercise == null)
            return BadRequest(new { message = "Failed to generate exercise. Please try again." });

        var response = new ExerciseResponse
        {
            Id = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            UnitTitle = exercise.CurriculumUnit?.UnitTitle,
            Difficulty = exercise.Difficulty,
            QuestionCount = exercise.QuestionCount,
            Questions = ExerciseGeneratorService.StripAnswers(exercise.Content, exercise.Section.Code),
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
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null) return NotFound();

        var hasSubmitted = await _db.ExerciseResults
            .AnyAsync(r => r.ExerciseId == id && r.UserId == userId);

        var response = new ExerciseResponse
        {
            Id = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            UnitTitle = exercise.CurriculumUnit?.UnitTitle,
            Difficulty = exercise.Difficulty,
            QuestionCount = exercise.QuestionCount,
            Questions = hasSubmitted
                ? JsonDocument.Parse(exercise.Content).RootElement.Clone()
                : ExerciseGeneratorService.StripAnswers(exercise.Content, exercise.Section.Code),
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
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null) return NotFound();

        var result = await _grading.GradeAsync(id, userId, request.UserAnswers, request.TimeTakenSeconds);
        if (result == null)
            return BadRequest(new { message = "Failed to grade exercise." });

        var response = new ExerciseResultResponse
        {
            ExerciseId = exercise.Id,
            SectionCode = exercise.Section.Code,
            SectionName = exercise.Section.Name,
            CorrectCount = result.CorrectCount,
            TotalQuestions = result.TotalQuestions,
            ScorePercent = result.ScorePercent,
            TimeTakenSeconds = result.TimeTakenSeconds,
            FullContent = JsonDocument.Parse(exercise.Content).RootElement.Clone(),
            UserAnswers = JsonDocument.Parse(result.UserAnswers).RootElement.Clone(),
            AiFeedback = result.AiFeedback,
            CompletedAt = result.CompletedAt
        };

        return Ok(response);
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
                Difficulty = e.Difficulty,
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
