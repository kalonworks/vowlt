namespace Vowlt.Api.Features.Search.DTOs;

public record SearchRequest
{
    public required string Query { get; init; }
    public int Limit { get; init; } = 20;
    public double MinimumScore { get; init; } = 0.5;  // Cosine similarity threshold (0-1)
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Domain { get; init; }
}

