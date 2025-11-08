using System.Text.Json;
using Microsoft.Extensions.Options;
using Vowlt.Api.Features.Embedding.Options;
using Vowlt.Api.Features.Embedding.DTOs;

namespace Vowlt.Api.Features.Embedding.Services;

public class EmbeddingService(
    HttpClient httpClient,
    IOptions<EmbeddingOptions> options,
    ILogger<EmbeddingService> logger) : IEmbeddingService
{
    private readonly EmbeddingOptions _options = options.Value;

    public async Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await EmbedTextsAsync([text], cancellationToken);
        return results[0];
    }

    public async Task<float[][]> EmbedTextsAsync(string[] texts, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Length == 0)
            throw new ArgumentException("Texts array cannot be null or empty", nameof(texts));

        try
        {
            logger.LogInformation(
                "Embedding {Count} texts using service at {Url}",
                texts.Length,
                _options.ServiceUrl);

            var request = new EmbedRequest(texts);

            var response = await httpClient.PostAsJsonAsync(
                "/embed",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Embedding service returned null response");

            logger.LogInformation(
                "Successfully embedded {Count} texts in {Time}ms using model {Model}",
                texts.Length,
                result.ProcessingTimeMs,
                result.Model);

            // Validate dimensions
            if (result.Dimensions != _options.VectorDimensions)
            {
                throw new InvalidOperationException(
                $"Expected {_options.VectorDimensions} dimensions but received {result.Dimensions}");
            }
            return result.Embeddings;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "HTTP error calling embedding service at {Url}",
                _options.ServiceUrl);
            throw new InvalidOperationException(
                $"Failed to call embedding service: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "Embedding request timed out after {Timeout} seconds",
                _options.TimeoutSeconds);
            throw new TimeoutException(
                $"Embedding service request timed out after {_options.TimeoutSeconds} seconds", ex);
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "Failed to deserialize embedding service response");
            throw new InvalidOperationException(
                "Invalid response from embedding service", ex);
        }
    }
}
