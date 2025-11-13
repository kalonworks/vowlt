namespace Vowlt.Api.Features.Search.Models;

/// <summary>
/// Internal model representing a result from vector (semantic) search
/// </summary>
public record VectorSearchResult
{
    public required Guid BookmarkId { get; init; }
    public required double Distance { get; init; }
    public required double SimilarityScore { get; init; }
    public required int Rank { get; init; }
}
