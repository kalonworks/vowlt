namespace Vowlt.Api.Features.Llm.Models;

public record LlmResponse(
    string Text,
    bool IsSuccess,
    string? ErrorMessage = null);
