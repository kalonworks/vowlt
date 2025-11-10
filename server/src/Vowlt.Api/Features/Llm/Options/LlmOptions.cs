namespace Vowlt.Api.Features.Llm.Options;

public class LlmOptions
{
    public const string SectionName = "Llm";

    public string Provider { get; init; } = "Gemini";
    public GeminiOptions Gemini { get; init; } = new();
}
