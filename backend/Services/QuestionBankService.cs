using System.Text.Json;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Services;

public class QuestionBankService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QuestionBankService> _logger;

    public QuestionBankService(IServiceProvider services, ILogger<QuestionBankService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Question Bank Producer Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateNextQuestionAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in QuestionBankService. Retrying in 10s...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            // Optional delay between generations to respect rate limits if needed.
            // If we want to run as fast as possible, just a small delay to avoid 100% CPU on empty iterations.
            await Task.Delay(2000, stoppingToken);
        }

        _logger.LogInformation("Question Bank Producer Service is stopping.");
    }

    private async Task GenerateNextQuestionAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var openRouter = scope.ServiceProvider.GetRequiredService<OpenRouterService>();

        // Min-First Balancing: find section with lowest available question count
        var stats = await db.ExerciseSections
            .Select(s => new
            {
                Section = s,
                AvailableCount = db.Set<Question>().Count(q => q.SectionId == s.Id && !q.IsAssigned)
            })
            .OrderBy(x => x.AvailableCount)
            .FirstOrDefaultAsync(stoppingToken);

        if (stats == null) return; // No sections

        // We could also do it by CurriculumUnit for grammar/vocab, but let's stick to Section-level general questions for now unless we loop over units too.
        // For simplicity: generate 1 question.
        var sectionCode = stats.Section.Code;
        var sectionId = stats.Section.Id;
        
        // We set questionCount = 1 for the prompt, except if it's reading/cloze, the AI will naturally generate 1 passage and multiple sub-questions.
        var systemPrompt = ExerciseGeneratorService.GetSystemPrompt();
        var userPrompt = ExerciseGeneratorService.BuildPrompt(sectionCode, null, 1);

        _logger.LogInformation("Bank Service: Generating 1 {SectionCode} question (Current Available: {AvailableCount})", sectionCode, stats.AvailableCount);

        var jsonDoc = await openRouter.ChatCompletionJsonAsync(systemPrompt, userPrompt);
        if (jsonDoc == null)
        {
            _logger.LogWarning("Bank Service: AI returned null for {SectionCode}", sectionCode);
            return;
        }

        var content = jsonDoc.RootElement.GetRawText();
        
        // Validate JSON
        try
        {
            JsonDocument.Parse(content);
        }
        catch
        {
            _logger.LogWarning("Bank Service: AI returned invalid JSON for {SectionCode}", sectionCode);
            return;
        }

        var question = new Question
        {
            SectionId = sectionId,
            Content = content,
            IsAssigned = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Set<Question>().Add(question);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Bank Service: Added new {SectionCode} question to bank.", sectionCode);
    }
}
