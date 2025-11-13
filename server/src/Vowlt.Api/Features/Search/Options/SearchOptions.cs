namespace Vowlt.Api.Features.Search.Options;

public class SearchOptions
{
    public const string SectionName = "Search";

    /// <summary>
    /// Default search mode when not specified in request
    /// </summary>
    public SearchMode DefaultMode { get; init; } = SearchMode.Hybrid;

    /// <summary>
    /// RRF k parameter for rank fusion (default: 60)
    /// Higher values = more equal weighting between methods
    /// </summary>
    public double RrfK { get; init; } = 60.0;

    /// <summary>
    /// Weight for vector search in weighted fusion (0-1)
    /// </summary>
    public double VectorWeight { get; init; } = 0.7;

    /// <summary>
    /// Weight for keyword search in weighted fusion (0-1)
    /// </summary>
    public double KeywordWeight { get; init; } = 0.3;

    /// <summary>
    /// Maximum results to fetch from keyword search
    /// </summary>
    public int MaxKeywordResults { get; init; } = 50;

    /// <summary>
    /// Maximum results to fetch from vector search
    /// </summary>
    public int MaxVectorResults { get; init; } = 50;

    /// <summary>
    /// Minimum BM25 score threshold for keyword search results
    /// Results below this score are filtered out (default: 1.0)
    /// </summary>
    public double MinimumBm25Score { get; init; } = 1.0;

    /// <summary>
    /// Minimum RRF score threshold after fusion
    /// Results below this score are filtered out (default: 0.015)
    /// Rank 5 in one search = 0.0154, Rank 10 = 0.0143
    /// </summary>
    public double MinimumRrfScore { get; init; } = 0.015;
}

public enum SearchMode
{
    /// <summary>
    /// Vector-only semantic search
    /// </summary>
    Vector,

    /// <summary>
    /// Keyword-only BM25 search
    /// </summary>
    Keyword,

    /// <summary>
    /// Hybrid search combining vector and keyword with RRF fusion
    /// </summary>
    Hybrid
}
