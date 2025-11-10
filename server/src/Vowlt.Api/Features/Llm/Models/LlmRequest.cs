namespace Vowlt.Api.Features.Llm.Models;

public record LlmRequest(
     string Prompt,
     double Temperature = 0.7,
     int MaxTokens = 100);


