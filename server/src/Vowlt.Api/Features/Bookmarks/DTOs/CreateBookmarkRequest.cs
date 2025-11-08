namespace Vowlt.Api.Features.Bookmarks.DTOs;

public record CreateBookmarkRequest
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public string? FullText { get; init; }
}
