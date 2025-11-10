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
    public List<string> Tags { get; private set; } = [];
    public List<string> GeneratedTags { get; private set; } = [];



    // EF Core requires parameterless constructor
    private Bookmark() { }

    // Factory method for creating new bookmarks
    public static Bookmark Create(
        Guid userId,
        string url,
        string title,
        DateTime now,
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
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // Update bookmark content
    public void Update(
        DateTime now,
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

        UpdatedAt = now;
    }

    // Set embedding from service
    public void SetEmbedding(float[] embeddingValues, int expectedDimensions = 384)
    {
        if (embeddingValues == null || embeddingValues.Length != expectedDimensions)
        {
            throw new ArgumentException(
                $"Embedding must be {expectedDimensions} dimensions but got {embeddingValues?.Length ?? 0}",
                nameof(embeddingValues));
        }

        Embedding = new Vector(embeddingValues);
    }

    // Set full text content (for hybrid search later)
    public void SetFullText(string fullText, DateTime now)
    {
        FullText = fullText;
        UpdatedAt = now;
    }

    // Set metadata
    public void SetMetadata(string? faviconUrl, string? ogImageUrl, DateTime now)
    {
        FaviconUrl = faviconUrl;
        OgImageUrl = ogImageUrl;
        UpdatedAt = now;
    }

    // Track access
    public void MarkAsAccessed(DateTime now)
    {
        LastAccessedAt = now;
    }

    // Get text for embedding (combines multiple fields)
    // Get text for embedding (combines multiple fields including tags)
    public string GetTextForEmbedding()
    {
        var parts = new List<string> { Title };

        if (!string.IsNullOrWhiteSpace(Description))
            parts.Add(Description);

        if (!string.IsNullOrWhiteSpace(Notes))
            parts.Add(Notes);

        if (Tags.Count > 0)
            parts.Add(string.Join(" ", Tags));

        if (GeneratedTags.Count > 0)
            parts.Add(string.Join(" ", GeneratedTags));

        return string.Join(" ", parts);
    }

    public void SetTags(List<string> tags, DateTime now)
    {
        Tags = tags ?? [];
        UpdatedAt = now;
    }

    // Set AI-generated tags
    public void SetGeneratedTags(List<string> tags, DateTime now)
    {
        GeneratedTags = tags ?? [];
        UpdatedAt = now;
    }

    // Get all tags (user + AI combined, distinct)
    public List<string> GetAllTags()
    {
        return Tags.Concat(GeneratedTags).Distinct().ToList();
    }
}

