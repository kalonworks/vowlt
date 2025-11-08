using Pgvector;

namespace Vowlt.Api.Features.Bookmarks.Models;

public class Bookmark
{
    // Primary key
    public Guid Id { get; private set; }

    // Multi-tenancy (every bookmark belongs to a user)
    public Guid UserId { get; private set; }

    // Core bookmark data
    public string Url { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Notes { get; private set; }

    // Content for search (optional - scraped article text)
    public string? FullText { get; private set; }

    // Vector embedding (384 dimensions for all-MiniLM-L6-v2)
    public Vector? Embedding { get; private set; }

    // Metadata
    public string? FaviconUrl { get; private set; }
    public string? OgImageUrl { get; private set; }
    public string? Domain { get; private set; }

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }

    // EF Core requires parameterless constructor
    private Bookmark() { }

    // Factory method for creating new bookmarks
    public static Bookmark Create(
        Guid userId,
        string url,
        string title,
        string? description = null,
        string? notes = null)
    {
        // Validation
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        // Extract domain from URL
        string? domain = null;
        try
        {
            var uri = new Uri(url);
            domain = uri.Host;
        }
        catch
        {
            // Invalid URL, domain stays null
        }

        return new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Url = url.Trim(),
            Title = title.Trim(),
            Description = description?.Trim(),
            Notes = notes?.Trim(),
            Domain = domain,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Update bookmark content
    public void Update(
        string? title = null,
        string? description = null,
        string? notes = null)
    {
        if (title is not null)
            Title = title.Trim();

        if (description is not null)
            Description = description.Trim();

        if (notes is not null)
            Notes = notes.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    // Set embedding from service
    public void SetEmbedding(float[] embeddingValues)
    {
        if (embeddingValues == null || embeddingValues.Length != 384)
            throw new ArgumentException("Embedding must be 384 dimensions", nameof(embeddingValues));

        Embedding = new Vector(embeddingValues);
        UpdatedAt = DateTime.UtcNow;
    }

    // Set full text content (for hybrid search later)
    public void SetFullText(string fullText)
    {
        FullText = fullText;
        UpdatedAt = DateTime.UtcNow;
    }

    // Set metadata
    public void SetMetadata(string? faviconUrl, string? ogImageUrl)
    {
        FaviconUrl = faviconUrl;
        OgImageUrl = ogImageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    // Track access
    public void MarkAsAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
    }

    // Get text for embedding (combines multiple fields)
    public string GetTextForEmbedding()
    {
        var parts = new List<string> { Title };

        if (!string.IsNullOrWhiteSpace(Description))
            parts.Add(Description);

        if (!string.IsNullOrWhiteSpace(Notes))
            parts.Add(Notes);

        return string.Join(" ", parts);
    }
}

