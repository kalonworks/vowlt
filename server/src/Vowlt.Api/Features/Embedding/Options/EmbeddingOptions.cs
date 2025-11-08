namespace Vowlt.Api.Features.Embedding.Options;

public class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string ServiceUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
