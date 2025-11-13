using Vowlt.Api.Features.Search.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Service for BM25 keyword search using ParadeDB
/// </summary>
public interface IKeywordSearchService
{
    /// <summary>
    /// Search bookmarks using BM25 keyword search
    /// </summary>
    /// <param name="userId">User ID for multi-tenant filtering</param>
    /// <param name="query">Search query (supports AND/OR, phrases, prefix matching)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="fromDate">Optional: Filter bookmarks created after this date</param>
    /// <param name="toDate">Optional: Filter bookmarks created before this date</param>
    /// <param name="domain">Optional: Filter bookmarks by domain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of keyword search results with BM25 scores and ranks</returns>
    Task<List<KeywordSearchResult>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? domain = null,
        CancellationToken cancellationToken = default);
}

