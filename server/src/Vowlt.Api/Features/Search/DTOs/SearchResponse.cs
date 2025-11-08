namespace Vowlt.Api.Features.Search.DTOs;

public record SearchResponse
{
    public required string Query { get; init; }
    public required IEnumerable<SearchResultDto> Results { get; init; }
    public required int TotalResults { get; init; }
    public required double ProcessingTimeMs { get; init; }
}

