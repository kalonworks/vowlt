using System.Text;
using System.Text.Json;
using Vowlt.Api.Features.Search.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Cross-encoder reranking service using external HTTP endpoint
/// </summary>
public class CrossEncoderService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<CrossEncoderService> logger) : ICrossEncoderService
{
    private readonly string _serviceUrl = configuration["Embedding:ServiceUrl"]
        ?? "http://localhost:8000";

    public async Task<List<double>> RerankAsync(
        string query,
        List<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var request = new RerankRequest
            {
                Query = query,
                Texts = texts
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            logger.LogInformation(
                "Reranking {Count} texts for query: {Query}",
                texts.Count, query);

            var response = await client.PostAsync(
                $"{_serviceUrl}/rerank",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<RerankResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (result?.Scores == null)
                throw new Exception("Invalid response from reranking service");

            // Extract scores in original order
            var scores = result.Scores
                .OrderBy(s => s.Index)
                .Select(s => s.Score)
                .ToList();

            logger.LogInformation(
                "Reranking completed: Min={Min:F3}, Max={Max:F3}, Avg={Avg:F3}",
                scores.Min(), scores.Max(), scores.Average());

            return scores;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cross-encoder reranking failed for query: {Query}", query);
            throw;
        }
    }
}

