using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Vowlt.Api.Features.Llm.Models;
using Vowlt.Api.Features.Llm.Options;

namespace Vowlt.Api.Features.Llm.Services;

public class GeminiLlmService(
    HttpClient httpClient,  // Changed from IHttpClientFactory
    IOptions<GeminiOptions> options,
    ILogger<GeminiLlmService> logger) : ILlmService
{
    private readonly GeminiOptions _options = options.Value;
    private readonly HttpClient _httpClient = httpClient;  // Store it

    public async Task<LlmResponse> GenerateAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the injected HttpClient directly (no factory needed)

            // Build the Gemini API request
            var geminiRequest = new GeminiApiRequest
            {
                Contents =
                [
                    new Content
                      {
                          Parts =
                          [
                              new Part { Text = request.Prompt }
                          ]
                      }
                ],
                GenerationConfig = new GenerationConfig
                {
                    Temperature = request.Temperature,
                    MaxOutputTokens = request.MaxTokens
                }
            };

            // Make the API call
            var endpoint = $"{_options.BaseUrl}/models/{_options.Model}:generateContent";
            var response = await _httpClient.PostAsJsonAsync(
                endpoint,
                geminiRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "Gemini API request failed with status {StatusCode}: {Error}",
                    response.StatusCode,
                    errorContent);

                return new LlmResponse(
                    string.Empty,
                    false,
                    $"API request failed: {response.StatusCode}");
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(
                cancellationToken: cancellationToken);

            if (geminiResponse?.Candidates is null || geminiResponse.Candidates.Count == 0)
            {
                logger.LogWarning("Gemini API returned no candidates");
                return new LlmResponse(
                    string.Empty,
                    false,
                    "No response generated");
            }

            var text = geminiResponse.Candidates[0].Content.Parts[0].Text;
            return new LlmResponse(text, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Gemini API");
            return new LlmResponse(
                string.Empty,
                false,
                $"Exception: {ex.Message}");
        }
    }

    // Gemini API request/response models (internal to this service)
    private record GeminiApiRequest
    {
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; init; } = [];

        [JsonPropertyName("generationConfig")]
        public GenerationConfig? GenerationConfig { get; init; }
    }

    private record Content
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; init; } = [];
    }

    private record Part
    {
        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;
    }

    private record GenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; init; }
    }

    private record GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; init; } = [];
    }

    private record Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; init; } = new();
    }
}
