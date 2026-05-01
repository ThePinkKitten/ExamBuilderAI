using System.Text.Json;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.Models;
using ExamBuilderAI.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Manually insert a question into the Question Bank. Useful for testing or manual data entry.
    /// </summary>
    [HttpPost("questions")]
    public async Task<IActionResult> AddQuestionToBank([FromBody] AddQuestionRequest request)
    {
        var section = await _db.ExerciseSections.FindAsync(request.SectionId);
        if (section == null) return NotFound(new { message = "Section not found." });

        var jsonContent = JsonSerializer.Serialize(request.Content);

        var question = new Question
        {
            SectionId = request.SectionId,
            Content = jsonContent,
            IsAssigned = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Questions.Add(question);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Question added to bank successfully.",
            questionId = question.Id,
            section = section.Code
        });
    }

    /// <summary>
    /// Ask the AI to generate a question and return its RAW output for debugging purposes.
    /// If the output is valid JSON, it will also save it to the database for convenience.
    /// </summary>
    [HttpPost("generate-test")]
    public async Task<IActionResult> TestAiGeneration([FromBody] TestAiRequest request, [FromServices] OpenRouterService openRouter)
    {
        var systemPrompt = ExamBuilderAI.API.Services.ExerciseGeneratorService.GetSystemPrompt();
        var userPrompt = ExamBuilderAI.API.Services.ExerciseGeneratorService.BuildPrompt(request.SectionCode, null, 1);

        // Get the FULL HTTP response string from OpenRouter
        var rawResponse = await openRouter.ChatCompletionRawResponseAsync(systemPrompt, userPrompt);

        if (string.IsNullOrEmpty(rawResponse))
        {
            return BadRequest(new { message = "AI returned null, empty, or timed out." });
        }

        bool savedToBank = false;
        
        // Attempt to extract the inner content and save it
        try
        {
            using var doc = JsonDocument.Parse(rawResponse);
            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                if (choices[0].TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentElement))
                {
                    var innerContent = contentElement.GetString();
                    if (!string.IsNullOrWhiteSpace(innerContent))
                    {
                        var cleanedContent = innerContent.Trim();
                        if (cleanedContent.StartsWith("```json")) cleanedContent = cleanedContent[7..];
                        else if (cleanedContent.StartsWith("```")) cleanedContent = cleanedContent[3..];
                        if (cleanedContent.EndsWith("```")) cleanedContent = cleanedContent[..^3];
                        cleanedContent = cleanedContent.Trim();

                        JsonDocument.Parse(cleanedContent); // Just checking if it throws
                        
                        var section = await _db.ExerciseSections.FirstOrDefaultAsync(s => s.Code == request.SectionCode);
                        if (section != null)
                        {
                            var question = new Question
                            {
                                SectionId = section.Id,
                                Content = cleanedContent,
                                IsAssigned = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.Questions.Add(question);
                            await _db.SaveChangesAsync();
                            savedToBank = true;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Invalid JSON, don't save it
        }

        object? rawOutputObj = null;
        try 
        {
            rawOutputObj = JsonSerializer.Deserialize<JsonElement>(rawResponse);
        } 
        catch 
        {
            rawOutputObj = rawResponse;
        }

        return Ok(new
        {
            sectionCode = request.SectionCode,
            savedToBank = savedToBank,
            rawOutput = rawOutputObj
        });
    }

    /// <summary>
    /// Get the current statistics of the Question Bank (how many questions are in each section).
    /// </summary>
    [HttpGet("bank-stats")]
    public async Task<IActionResult> GetBankStats()
    {
        var stats = await _db.ExerciseSections
            .Select(s => new
            {
                SectionId = s.Id,
                SectionCode = s.Code,
                SectionName = s.Name,
                TotalInBank = _db.Questions.Count(q => q.SectionId == s.Id),
                AvailableCount = _db.Questions.Count(q => q.SectionId == s.Id && !q.IsAssigned)
            })
            .OrderBy(s => s.SectionId)
            .ToListAsync();

        return Ok(stats);
    }
}

public class TestAiRequest
{
    public required string SectionCode { get; set; }
}

public class AddQuestionRequest
{
    public int SectionId { get; set; }
    public required JsonElement Content { get; set; }
}
