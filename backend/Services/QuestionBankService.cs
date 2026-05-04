using System.Text.Json;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Services;

public class QuestionBankService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QuestionBankService> _logger;
    private readonly IConfiguration _config;

    public QuestionBankService(IServiceProvider services, ILogger<QuestionBankService> logger, IConfiguration config)
    {
        _services = services;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Question Bank Producer Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isEnabled = _config.GetValue<bool>("QuestionBank:Enabled");
                if (isEnabled)
                {
                    await GenerateNextQuestionAsync(stoppingToken);
                }
                else
                {
                    // If disabled, check less frequently to save resources
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in QuestionBankService. Retrying in 60s...");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            await Task.Delay(30000, stoppingToken);
        }

        _logger.LogInformation("Question Bank Producer Service is stopping.");
    }

    private async Task GenerateNextQuestionAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var provider = _config["AIProvider"] ?? "OpenRouter";
        const int MAX_PER_COMBINATION = 100;

        // 1. Gather all potential (Section, Unit) combinations
        var sections = await db.ExerciseSections.ToListAsync(stoppingToken);
        var units = await db.CurriculumUnits.ToListAsync(stoppingToken);
        
        var combinations = new List<(ExerciseSection section, int? unitId, string unitTitle, int count)>();

        foreach (var s in sections)
        {
            // General unit for this section
            int genCount = await db.Questions.CountAsync(q => q.SectionId == s.Id && q.CurriculumUnitId == null && !q.IsAssigned, stoppingToken);
            combinations.Add((s, null, "General", genCount));

            foreach (var u in units)
            {
                int count = await db.Questions.CountAsync(q => q.SectionId == s.Id && q.CurriculumUnitId == u.Id && !q.IsAssigned, stoppingToken);
                combinations.Add((s, u.Id, u.UnitTitle, count));
            }
        }

        // 2. Find the one with the FEWEST questions that is still UNDER the limit
        var target = combinations
            .Where(x => x.count < MAX_PER_COMBINATION)
            .OrderBy(x => x.count)
            .FirstOrDefault();

        if (target.section == null)
        {
            _logger.LogInformation("Bank Service: All (Section, Unit) combinations have reached the limit of {Limit}. Sleeping...", MAX_PER_COMBINATION);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            return;
        }

        CurriculumUnit? targetUnit = target.unitId.HasValue ? units.First(u => u.Id == target.unitId.Value) : null;

        // 3. Generate prompt
        var systemPrompt = ExerciseGeneratorService.GetSystemPrompt();
        var userPrompt = ExerciseGeneratorService.BuildPrompt(target.section.Code, targetUnit, 1);

        _logger.LogInformation("Bank Service: Generating 1 {SectionCode} question for {UnitTitle} (Current: {Count}/{Limit})", 
            target.section.Code, target.unitTitle, target.count, MAX_PER_COMBINATION);

        JsonDocument? jsonDoc = null;

        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            var gemini = scope.ServiceProvider.GetRequiredService<GeminiService>();
            jsonDoc = await gemini.GenerateContentAsync(systemPrompt, userPrompt, forceJson: true, cancellationToken: stoppingToken);
        }
        else
        {
            var openRouter = scope.ServiceProvider.GetRequiredService<OpenRouterService>();
            jsonDoc = await openRouter.ChatCompletionJsonAsync(systemPrompt, userPrompt, cancellationToken: stoppingToken);
        }

        if (jsonDoc == null)
        {
            _logger.LogWarning("Bank Service: AI returned null for {SectionCode}", target.section.Code);
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
            _logger.LogWarning("Bank Service: AI returned invalid JSON for {SectionCode}", target.section.Code);
            return;
        }

        var question = new Question
        {
            SectionId = target.section.Id,
            CurriculumUnitId = target.unitId,
            Content = content,
            IsAssigned = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Questions.Add(question);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Bank Service: Added new {SectionCode} question for {UnitTitle} to bank.", 
            target.section.Code, target.unitTitle);
    }
}
