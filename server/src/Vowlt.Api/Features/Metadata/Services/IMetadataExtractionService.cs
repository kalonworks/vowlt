using Vowlt.Api.Features.Metadata.Models;

namespace Vowlt.Api.Features.Metadata.Services;

/// <summary>
/// Service for extracting metadata from web pages.
/// </summary>
public interface IMetadataExtractionService
{
    /// <summary>
    /// Extracts metadata from a URL asynchronously.
    /// Returns null if extraction fails (non-blocking pattern).
    /// </summary>
    /// <param name="url">The URL to extract metadata from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted metadata, or null if extraction fails</returns>
    Task<PageMetadata?> ExtractMetadataAsync(
        string url,
        CancellationToken cancellationToken = default);
}
