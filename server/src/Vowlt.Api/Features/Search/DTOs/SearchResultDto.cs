namespace Vowlt.Api.Features.Search.DTOs;

public record SearchResultDto
{
    public required Guid Id { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Domain { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required double SimilarityScore { get; init; }

    /// <summary>
    /// BM25 score from keyword search (Keyword/Hybrid mode)
    /// </summary>
    public double? KeywordScore { get; init; }

    /// <summary>
    /// Cosine similarity score from vector search (Vector/Hybrid mode)
    /// </summary>
    public double? VectorScore { get; init; }

    /// <summary>
    /// RRF (Reciprocal Rank Fusion) score (Hybrid mode only)
    /// </summary>
    public double? HybridScore { get; init; }

    /// <summary>
    /// Rank in keyword search results (Hybrid mode only)
    /// </summary>
    public int? KeywordRank { get; init; }

    /// <summary>
    /// Rank in vector search results (Hybrid mode only)
    /// </summary>
    public int? VectorRank { get; init; }
}
