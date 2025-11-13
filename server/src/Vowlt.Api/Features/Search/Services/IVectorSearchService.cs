using Vowlt.Api.Features.Search.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Service for vector (semantic) search using pgvector
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Search bookmarks using vector similarity (semantic search)
    /// </summary>
    /// <param name="userId">User ID for multi-tenant filtering</param>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="minimumScore">Minimum similarity score (0-1)</param>
    /// <param name="fromDate">Optional: Filter bookmarks created after this date</param>
    /// <param name="toDate">Optional: Filter bookmarks created before this date</param>
    /// <param name="domain">Optional: Filter bookmarks by domain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of vector search results with similarity scores and ranks</returns>
    Task<List<VectorSearchResult>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        double minimumScore = 0.5,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? domain = null,
        CancellationToken cancellationToken = default);
}
