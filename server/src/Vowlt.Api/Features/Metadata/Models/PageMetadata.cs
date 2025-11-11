namespace Vowlt.Api.Features.Metadata.Models;

/// <summary>
/// Represents metadata extracted from a web page.
/// Includes Open Graph, Twitter Card, and standard HTML metadata.
/// </summary>
public record PageMetadata
{
    // Open Graph metadata (primary - most sites support this)
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImage { get; init; }
    public string? OgSiteName { get; init; }

    // Twitter Card metadata (secondary - Twitter/X specific)
    public string? TwitterTitle { get; init; }
    public string? TwitterDescription { get; init; }
    public string? TwitterImage { get; init; }

    // Standard HTML fallbacks (when OG/Twitter tags don't exist)
    public string? HtmlTitle { get; init; }
    public string? MetaDescription { get; init; }

    // Favicon
    public string? FaviconUrl { get; init; }

    // Best values (computed with fallback logic: OG → Twitter → HTML)
    public string? BestTitle { get; init; }
    public string? BestDescription { get; init; }
    public string? BestImage { get; init; }

    // Extraction metadata (for debugging/monitoring)
    public DateTime ExtractedAt { get; init; }
    public TimeSpan ExtractionDuration { get; init; }
}

