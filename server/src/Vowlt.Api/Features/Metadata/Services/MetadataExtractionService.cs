using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Options;
using Vowlt.Api.Features.Metadata.Models;
using Vowlt.Api.Features.Metadata.Options;

namespace Vowlt.Api.Features.Metadata.Services;

/// <summary>
/// Extracts metadata from web pages using AngleSharp HTML parser.
/// Supports Open Graph, Twitter Card, and standard HTML metadata.
/// </summary>
public class MetadataExtractionService(
    HttpClient httpClient,
    IOptions<MetadataExtractionOptions> options,
    ILogger<MetadataExtractionService> logger) : IMetadataExtractionService
{
    private readonly MetadataExtractionOptions _options = options.Value;

    public async Task<PageMetadata?> ExtractMetadataAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            logger.LogInformation("Extracting metadata from {Url}", url);

            // Step 1: Fetch HTML content from the URL
            var html = await FetchHtmlAsync(url, cancellationToken);

            if (string.IsNullOrEmpty(html))
            {
                logger.LogWarning("No HTML content received from {Url}", url);
                return null;
            }

            // Step 2: Parse HTML with AngleSharp
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(
                req => req.Content(html),
                cancellationToken);

            // Step 3: Extract metadata from the parsed document
            var metadata = ExtractMetadataFromDocument(document, url);

            // Step 4: Add extraction timing metadata
            var duration = DateTime.UtcNow - startTime;
            var result = metadata with
            {
                ExtractedAt = DateTime.UtcNow,
                ExtractionDuration = duration
            };

            logger.LogInformation(
                "Successfully extracted metadata from {Url} in {Duration}ms. " +
                "Title={HasTitle}, Description={HasDesc}, Image={HasImage}",
                url,
                duration.TotalMilliseconds,
                !string.IsNullOrEmpty(result.BestTitle),
                !string.IsNullOrEmpty(result.BestDescription),
                !string.IsNullOrEmpty(result.BestImage));

            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to extract metadata from {Url}. This is non-blocking.",
                url);
            return null;
        }
    }

    /// <summary>
    /// Fetches HTML content from a URL via HTTP GET.
    /// Returns null if request fails or content is not HTML.
    /// </summary>
    private async Task<string?> FetchHtmlAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "HTTP {StatusCode} when fetching metadata from {Url}",
                    response.StatusCode,
                    url);
                return null;
            }

            // Validate content type (only process HTML)
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null &&
                !contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Non-HTML content type ({ContentType}) from {Url}. Skipping metadata extraction.",
                    contentType,
                    url);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch HTML from {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Extracts metadata from an AngleSharp document.
    /// Applies fallback logic: Open Graph → Twitter → HTML.
    /// </summary>
    private PageMetadata ExtractMetadataFromDocument(IDocument document, string baseUrl)
    {
        // Extract Open Graph tags (most common standard)
        var ogTitle = GetMetaProperty(document, "og:title");
        var ogDescription = GetMetaProperty(document, "og:description");
        var ogImage = GetMetaProperty(document, "og:image");
        var ogSiteName = GetMetaProperty(document, "og:site_name");

        // Extract Twitter Card tags (Twitter/X specific)
        var twitterTitle = GetMetaName(document, "twitter:title");
        var twitterDescription = GetMetaName(document, "twitter:description");
        var twitterImage = GetMetaName(document, "twitter:image");

        // Extract standard HTML tags (fallbacks)
        var htmlTitle = document.QuerySelector("title")?.TextContent?.Trim();
        var metaDescription = GetMetaName(document, "description");

        // Extract favicon
        string? faviconUrl = null;
        if (_options.ExtractFavicon)
        {
            faviconUrl = ExtractFavicon(document, baseUrl);
        }

        // Apply fallback logic for best values (priority: OG → Twitter → HTML)
        var bestTitle = ogTitle ?? twitterTitle ?? htmlTitle;
        var bestDescription = ogDescription ?? twitterDescription ?? metaDescription;
        var bestImage = ogImage ?? twitterImage;

        // Resolve relative URLs to absolute URLs
        if (!string.IsNullOrEmpty(bestImage))
        {
            bestImage = ResolveUrl(baseUrl, bestImage);
        }

        if (!string.IsNullOrEmpty(faviconUrl))
        {
            faviconUrl = ResolveUrl(baseUrl, faviconUrl);
        }

        return new PageMetadata
        {
            // Open Graph
            OgTitle = ogTitle,
            OgDescription = ogDescription,
            OgImage = ogImage,
            OgSiteName = ogSiteName,

            // Twitter
            TwitterTitle = twitterTitle,
            TwitterDescription = twitterDescription,
            TwitterImage = twitterImage,

            // HTML
            HtmlTitle = htmlTitle,
            MetaDescription = metaDescription,

            // Favicon
            FaviconUrl = faviconUrl,

            // Best values (with fallback applied)
            BestTitle = bestTitle,
            BestDescription = bestDescription,
            BestImage = bestImage
        };
    }

    /// <summary>
    /// Gets content of a meta tag with property attribute.
    /// Example: &lt;meta property="og:title" content="Page Title" /&gt;
    /// </summary>
    private static string? GetMetaProperty(IDocument document, string property)
    {
        return document
            .QuerySelector($"meta[property='{property}']")
            ?.GetAttribute("content")
            ?.Trim();
    }

    /// <summary>
    /// Gets content of a meta tag with name attribute.
    /// Example: &lt;meta name="description" content="Page description" /&gt;
    /// </summary>
    private static string? GetMetaName(IDocument document, string name)
    {
        return document
            .QuerySelector($"meta[name='{name}']")
            ?.GetAttribute("content")
            ?.Trim();
    }

    /// <summary>
    /// Extracts favicon URL from HTML.
    /// Tries multiple common link rel values in priority order.
    /// </summary>
    private static string? ExtractFavicon(IDocument document, string baseUrl)
    {
        // Try common favicon locations in order of preference
        var selectors = new[]
        {
              "link[rel='icon']",
              "link[rel='shortcut icon']",
              "link[rel='apple-touch-icon']"
          };

        foreach (var selector in selectors)
        {
            var href = document.QuerySelector(selector)?.GetAttribute("href");
            if (!string.IsNullOrEmpty(href))
                return href;
        }

        // Fallback: /favicon.ico (standard location)
        return "/favicon.ico";
    }

    /// <summary>
    /// Resolves a relative URL to an absolute URL using a base URL.
    /// Example: base="https://example.com/blog", relative="/image.jpg" 
    ///          → result="https://example.com/image.jpg"
    /// </summary>
    private static string? ResolveUrl(string baseUrl, string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return null;

        try
        {
            var baseUri = new Uri(baseUrl);
            var absoluteUri = new Uri(baseUri, relativeUrl);
            return absoluteUri.ToString();
        }
        catch
        {
            // If resolution fails, return the original URL
            return relativeUrl;
        }
    }
}

