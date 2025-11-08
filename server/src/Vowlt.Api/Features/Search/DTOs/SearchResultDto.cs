namespace Vowlt.Api.Features.Search.DTOs;

public record SearchResultDto
{
    public required Guid Id { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Domain { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required double SimilarityScore { get; init; }  // 0-1, higher = more similar
}

