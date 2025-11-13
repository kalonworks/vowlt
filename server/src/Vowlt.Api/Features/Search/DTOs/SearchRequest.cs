using Vowlt.Api.Features.Search.Options;

namespace Vowlt.Api.Features.Search.DTOs;

public record SearchRequest
{
    public required string Query { get; init; }
    public int Limit { get; init; } = 20;
    public double MinimumScore { get; init; } = 0.5;
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Domain { get; init; }

    /// <summary>
    /// Search mode: Vector (semantic), Keyword (BM25), or Hybrid (both with RRF fusion)
    /// If null, uses the default mode from configuration
    /// </summary>
    public SearchMode? Mode { get; init; }
}

