
namespace Vowlt.Api.Features.Search.Models;

/// <summary>
/// Internal model representing a result from keyword (BM25) search
/// </summary>
public record KeywordSearchResult
{
    public required Guid BookmarkId { get; init; }
    public required double Bm25Score { get; init; }
    public required int Rank { get; init; }
}

