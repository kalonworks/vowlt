namespace Vowlt.Api.Features.Bookmarks.DTOs;

public record BookmarkDto
{
    public required Guid Id { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public string? FullText { get; init; }
    public string? FaviconUrl { get; init; }
    public string? OgImageUrl { get; init; }
    public string? Domain { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public DateTime? LastAccessedAt { get; init; }
    public bool HasEmbedding { get; init; }
}
