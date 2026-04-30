using System.Security.Claims;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.DTOs.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProgressController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get overall progress overview for the current user.
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var userId = GetUserId();

        var results = await _db.ExerciseResults
            .Where(r => r.UserId == userId)
            .Include(r => r.Exercise)
                .ThenInclude(e => e.Section)
            .ToListAsync();

        var sectionStats = results
            .GroupBy(r => r.Exercise.Section)
            .Select(g => new SectionProgress
            {
                SectionCode = g.Key.Code,
                SectionName = g.Key.Name,
                ExercisesDone = g.Count(),
                AverageScore = Math.Round(g.Average(r => r.ScorePercent), 1),
                TotalQuestions = g.Sum(r => r.TotalQuestions),
                CorrectAnswers = g.Sum(r => r.CorrectCount)
            })
            .OrderBy(s => s.SectionCode)
            .ToList();

        var overview = new ProgressOverview
        {
            TotalExercisesDone = results.Count,
            OverallAverageScore = results.Any() ? Math.Round(results.Average(r => r.ScorePercent), 1) : 0,
            TotalQuestionsAnswered = results.Sum(r => r.TotalQuestions),
            TotalCorrectAnswers = results.Sum(r => r.CorrectCount),
            SectionStats = sectionStats
        };

        return Ok(overview);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim ?? "0");
    }
}
