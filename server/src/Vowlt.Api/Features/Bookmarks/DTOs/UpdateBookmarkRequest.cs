namespace Vowlt.Api.Features.Bookmarks.DTOs;

public record UpdateBookmarkRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public string? FullText { get; init; }
    public List<string>? Tags { get; init; }
}

