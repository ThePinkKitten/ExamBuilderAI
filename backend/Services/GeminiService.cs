using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ExamBuilderAI.API.Services;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient http, IConfiguration config, ILogger<GeminiService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<JsonDocument?> GenerateContentAsync(string systemPrompt, string userPrompt, bool forceJson = true, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var model = _config["Gemini:Model"] ?? "gemini-2.5-flash";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Gemini API key is not configured.");
            return null;
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = forceJson ? new { response_mime_type = "application/json" } : null
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync(url, content, cancellationToken);
            var responseStr = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API Error: {StatusCode} - {Body}", response.StatusCode, responseStr);
                return null;
            }

            using var doc = JsonDocument.Parse(responseStr);
            var root = doc.RootElement;
            
            // Navigate the Gemini response structure: candidates[0].content.parts[0].text
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var resContent) && 
                    resContent.TryGetProperty("parts", out var parts) && 
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        return JsonDocument.Parse(text);
                    }
                }
            }

            _logger.LogError("Failed to parse Gemini response structure: {Body}", responseStr);
            return null;
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation during shutdown
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return null;
        }
    }
}
