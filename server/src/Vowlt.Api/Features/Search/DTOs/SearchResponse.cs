using Vowlt.Api.Features.Search.Options;

namespace Vowlt.Api.Features.Search.DTOs;

public record SearchResponse
{
    public required string Query { get; init; }
    public required IEnumerable<SearchResultDto> Results { get; init; }
    public required int TotalResults { get; init; }
    public required double ProcessingTimeMs { get; init; }

    /// <summary>
    /// Which search mode was used for this search
    /// </summary>
    public required SearchMode Mode { get; init; }

    /// <summary>
    /// Number of results from keyword search (Hybrid mode only)
    /// </summary>
    public int? KeywordResultCount { get; init; }

    /// <summary>
    /// Number of results from vector search (Hybrid mode only)
    /// </summary>
    public int? VectorResultCount { get; init; }
}
