using System.Text;
using System.Text.Json;

namespace ExamBuilderAI.API.Services;

public class OpenRouterService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenRouterService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OpenRouterService(HttpClient httpClient, IConfiguration config, ILogger<OpenRouterService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat completion request to OpenRouter and return the response content as a string.
    /// </summary>
    public async Task<string?> ChatCompletionAsync(string systemPrompt, string userPrompt, int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        var model = _config["OpenRouter:Model"] ?? "google/gemini-2.0-flash-exp:free";
        var apiKey = _config["OpenRouter:ApiKey"] ?? throw new InvalidOperationException("OpenRouter:ApiKey is not configured");

        var useJsonMode = !string.Equals(_config["OpenRouter:UseJsonMode"], "false", StringComparison.OrdinalIgnoreCase);
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = 0.7,
            ["max_tokens"] = 4096
        };

        if (useJsonMode)
        {
            requestBody["response_format"] = new { type = "json_object" };
        }

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var json = JsonSerializer.Serialize(requestBody, JsonOptions);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Headers.Add("HTTP-Referer", "https://exambuilder-ai.local");
                request.Headers.Add("X-Title", "ExamBuilder AI");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenRouter API error (attempt {Attempt}/{MaxRetries}): {StatusCode} - {Content}",
                        attempt, maxRetries, response.StatusCode, responseContent);

                    if (attempt < maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2)); // Exponential backoff
                        continue;
                    }
                    return null;
                }

                // Parse the response to extract the message content safely
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && 
                        message.TryGetProperty("content", out var contentElement))
                    {
                        var messageContent = contentElement.GetString();
                        if (!string.IsNullOrWhiteSpace(messageContent))
                        {
                            return messageContent;
                        }
                    }
                }

                _logger.LogWarning("OpenRouter returned unexpected JSON structure or empty content (attempt {Attempt}/{MaxRetries}). Response: {ResponseContent}", attempt, maxRetries, responseContent);
                if (attempt < maxRetries) continue;
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenRouter request failed (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                    continue;
                }
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Send a chat completion request to OpenRouter and return the RAW HTTP response body.
    /// Useful for debugging and extracting reasoning from models like DeepSeek-R1.
    /// </summary>
    public async Task<string?> ChatCompletionRawResponseAsync(string systemPrompt, string userPrompt)
    {
        var model = _config["OpenRouter:Model"] ?? "google/gemini-2.0-flash-exp:free";
        var apiKey = _config["OpenRouter:ApiKey"] ?? throw new InvalidOperationException("OpenRouter:ApiKey is not configured");

        var useJsonMode = !string.Equals(_config["OpenRouter:UseJsonMode"], "false", StringComparison.OrdinalIgnoreCase);
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = 0.7,
            ["max_tokens"] = 4096
        };

        if (useJsonMode)
        {
            requestBody["response_format"] = new { type = "json_object" };
        }

        try
        {
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("HTTP-Referer", "https://exambuilder-ai.local");
            request.Headers.Add("X-Title", "ExamBuilder AI");

            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter raw request failed");
            return null;
        }
    }

    /// <summary>
    /// Send a chat completion and parse the JSON response directly.
    /// Returns null if parsing fails after retries.
    /// </summary>
    public async Task<JsonDocument?> ChatCompletionJsonAsync(string systemPrompt, string userPrompt, int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        JsonException? lastError = null;
        string? lastContent = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var content = await ChatCompletionAsync(systemPrompt, userPrompt, maxRetries: 1, cancellationToken);
            if (content == null) continue;

            lastContent = content;
            content = StripMarkdownCodeBlock(content);

            if (TryParseJson(content, out var doc, out lastError))
                return doc;

            var extracted = ExtractJsonObject(content);
            if (extracted != null && TryParseJson(extracted, out doc, out lastError))
                return doc;

            _logger.LogWarning("Failed to parse OpenRouter response as JSON (attempt {Attempt}/{MaxRetries}).", attempt, maxRetries);
        }

        if (lastError != null)
        {
            _logger.LogError(lastError, "Failed to parse OpenRouter response as JSON after {MaxRetries} attempts. Content: {Content}", maxRetries, lastContent);
        }

        return null;
    }

    /// <summary>
    /// Send a chat completion request with stream: true and yield tokens as they arrive.
    /// </summary>
    public async IAsyncEnumerable<string> ChatCompletionStreamAsync(string systemPrompt, string userPrompt)
    {
        var model = _config["OpenRouter:Model"] ?? "google/gemini-2.0-flash-exp:free";
        var apiKey = _config["OpenRouter:ApiKey"] ?? throw new InvalidOperationException("OpenRouter:ApiKey is not configured");

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = 0.7,
            ["max_tokens"] = 4096,
            ["stream"] = true
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Headers.Add("HTTP-Referer", "https://exambuilder-ai.local");
        request.Headers.Add("X-Title", "ExamBuilder AI");

        // Use ResponseHeadersRead to process the response stream immediately
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("OpenRouter Stream API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;

                string? contentToYield = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentElement))
                        {
                            var content = contentElement.GetString();
                            if (!string.IsNullOrEmpty(content))
                            {
                                contentToYield = content;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignore malformed JSON chunks
                }

                if (contentToYield != null)
                {
                    yield return contentToYield;
                }
            }
        }
    }

    /// <summary>
    /// Send a vision request with an image (base64) for exam image analysis.
    /// </summary>
    public async Task<string?> VisionCompletionAsync(string systemPrompt, string userPrompt, string base64Image, string mimeType = "image/jpeg")
    {
        var model = _config["OpenRouter:VisionModel"] ?? "google/gemini-2.0-flash-exp:free";
        var apiKey = _config["OpenRouter:ApiKey"] ?? throw new InvalidOperationException("OpenRouter:ApiKey is not configured");

        var requestBody = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userPrompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:{mimeType};base64,{base64Image}" }
                        }
                    }
                }
            },
            temperature = 0.3,
            max_tokens = 4096
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("HTTP-Referer", "https://exambuilder-ai.local");
            request.Headers.Add("X-Title", "ExamBuilder AI");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenRouter Vision API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter Vision request failed");
            return null;
        }
    }

    private static string StripMarkdownCodeBlock(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```json"))
            content = content[7..];
        else if (content.StartsWith("```"))
            content = content[3..];

        if (content.EndsWith("```"))
            content = content[..^3];

        return content.Trim();
    }

    private static bool TryParseJson(string content, out JsonDocument? doc, out JsonException? error)
    {
        try
        {
            doc = JsonDocument.Parse(content);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            doc = null;
            error = ex;
            return false;
        }
    }

    private static string? ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        return content.Substring(start, end - start + 1);
    }
}
