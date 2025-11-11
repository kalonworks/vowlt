namespace Vowlt.Api.Features.Metadata.Options;

/// <summary>
/// Configuration options for metadata extraction service.
/// </summary>
public record MetadataExtractionOptions
{
    public const string SectionName = "MetadataExtraction";

    // HTTP client settings
    public int TimeoutSeconds { get; init; } = 15;
    public int MaxRetries { get; init; } = 2;
    public int RetryDelayMs { get; init; } = 1000;

    // User agent for web scraping (modern Chrome browser string)
    public string UserAgent { get; init; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    // Content limits
    public int MaxContentLengthMb { get; init; } = 5;
    public int MaxRedirects { get; init; } = 5;

    // Feature flags
    public bool FollowRedirects { get; init; } = true;
    public bool ExtractFavicon { get; init; } = true;
}
