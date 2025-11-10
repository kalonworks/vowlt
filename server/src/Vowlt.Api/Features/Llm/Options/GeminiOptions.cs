namespace Vowlt.Api.Features.Llm.Options;

public class GeminiOptions
{
    public string Model { get; init; } = "gemini-2.5-flash-lite";
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
}

