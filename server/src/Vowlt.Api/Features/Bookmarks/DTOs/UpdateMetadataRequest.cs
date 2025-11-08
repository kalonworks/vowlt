namespace Vowlt.Api.Features.Bookmarks.DTOs;

public record UpdateMetadataRequest
{
    public string? FaviconUrl { get; init; }
    public string? OgImageUrl { get; init; }
}
