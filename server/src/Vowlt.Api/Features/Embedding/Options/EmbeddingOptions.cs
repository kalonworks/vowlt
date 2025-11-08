namespace Vowlt.Api.Features.Embedding.Options;

public record EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string ServiceUrl { get; init; } = "http://localhost:8000";
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
    public int RetryDelayMs { get; init; } = 1000;
    public int VectorDimensions { get; init; } = 384;  // all-MiniLM-L6-v2 model dimensions
}

