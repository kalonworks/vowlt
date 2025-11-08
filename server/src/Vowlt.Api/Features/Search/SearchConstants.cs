namespace Vowlt.Api.Features.Search;

/// <summary>
/// Constants related to vector similarity search operations.
/// These are mathematical invariants and display preferences that don't change per environment.
/// </summary>
public static class SearchConstants
{
    /// <summary>
    /// Cosine distance returns values in range [0, 2] where:
    /// - 0 = identical vectors (similarity = 1.0)
    /// - 2 = opposite vectors (similarity = 0.0)
    /// We normalize by dividing by 2 to convert distance to similarity score [0, 1].
    /// This is a mathematical property that never changes.
    /// </summary>
    public const double CosineDistanceNormalizationFactor = 2.0;

    /// <summary>
    /// Number of decimal places to round similarity scores to in API responses.
    /// 4 decimals provides sufficient precision (e.g., 0.9234) without excessive noise.
    /// </summary>
    public const int SimilarityScoreDecimalPlaces = 4;
}

