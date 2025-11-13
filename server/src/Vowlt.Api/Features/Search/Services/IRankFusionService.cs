using Vowlt.Api.Features.Search.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Service for fusing results from multiple search methods using Reciprocal Rank Fusion (RRF)
/// </summary>
public interface IRankFusionService
{
    /// <summary>
    /// Fuse vector and keyword search results using RRF algorithm
    /// </summary>
    /// <param name="vectorResults">Results from vector search</param>
    /// <param name="keywordResults">Results from keyword search</param>
    /// <param name="k">RRF k parameter (default: 60). Higher = more equal weighting between methods</param>
    /// <returns>Fused results sorted by RRF score (highest first)</returns>
    List<FusedSearchResult> FuseResults(
        List<VectorSearchResult> vectorResults,
        List<KeywordSearchResult> keywordResults,
        double k = 60.0);
}
