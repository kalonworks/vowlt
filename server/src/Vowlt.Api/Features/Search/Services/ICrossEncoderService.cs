namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Service for reranking search results using cross-encoder model
/// </summary>
public interface ICrossEncoderService
{
    /// <summary>
    /// Rerank a list of texts based on relevance to the query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="texts">List of texts to rerank</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scores in same order as input texts (0-1 range)</returns>
    Task<List<double>> RerankAsync(
        string query,
        List<string> texts,
        CancellationToken cancellationToken = default);
}
