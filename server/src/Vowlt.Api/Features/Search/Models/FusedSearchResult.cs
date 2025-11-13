namespace Vowlt.Api.Features.Search.Models;

/// <summary>
/// Internal model representing a result after RRF fusion of vector and keyword results
/// </summary>
public record FusedSearchResult
{
    public required Guid BookmarkId { get; init; }
    public required double RrfScore { get; init; }
    public int? VectorRank { get; init; }
    public int? KeywordRank { get; init; }
    public double? VectorScore { get; init; }
    public double? KeywordScore { get; init; }
}
