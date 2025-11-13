using Vowlt.Api.Features.Search.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Implementation of Reciprocal Rank Fusion (RRF) for combining multiple search results
/// </summary>
public class RankFusionService : IRankFusionService
{
    /// <summary>
    /// Fuse vector and keyword search results using RRF algorithm
    /// RRF formula: RRF_Score(d) = Σ 1 / (k + rank_method(d))
    /// </summary>
    public List<FusedSearchResult> FuseResults(
        List<VectorSearchResult> vectorResults,
        List<KeywordSearchResult> keywordResults,
        double k = 60.0)
    {
        // Create dictionaries for fast lookup by BookmarkId
        var vectorDict = vectorResults.ToDictionary(r => r.BookmarkId);
        var keywordDict = keywordResults.ToDictionary(r => r.BookmarkId);

        // Get all unique bookmark IDs from both result sets
        var allBookmarkIds = vectorResults.Select(r => r.BookmarkId)
            .Union(keywordResults.Select(r => r.BookmarkId))
            .ToHashSet();

        // Calculate RRF score for each bookmark
        var fusedResults = new List<FusedSearchResult>();

        foreach (var bookmarkId in allBookmarkIds)
        {
            double rrfScore = 0.0;
            int? vectorRank = null;
            int? keywordRank = null;
            double? vectorScore = null;
            double? keywordScore = null;

            // Add contribution from vector search (if present)
            if (vectorDict.TryGetValue(bookmarkId, out var vectorResult))
            {
                vectorRank = vectorResult.Rank;
                vectorScore = vectorResult.SimilarityScore;
                rrfScore += 1.0 / (k + vectorResult.Rank);
            }

            // Add contribution from keyword search (if present)
            if (keywordDict.TryGetValue(bookmarkId, out var keywordResult))
            {
                keywordRank = keywordResult.Rank;
                keywordScore = keywordResult.Bm25Score;
                rrfScore += 1.0 / (k + keywordResult.Rank);
            }

            fusedResults.Add(new FusedSearchResult
            {
                BookmarkId = bookmarkId,
                RrfScore = rrfScore,
                VectorRank = vectorRank,
                KeywordRank = keywordRank,
                VectorScore = vectorScore,
                KeywordScore = keywordScore
            });
        }

        // Sort by RRF score (highest first) and return
        return fusedResults
            .OrderByDescending(r => r.RrfScore)
            .ToList();
    }
}
