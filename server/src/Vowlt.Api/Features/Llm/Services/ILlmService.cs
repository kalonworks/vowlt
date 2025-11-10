using Vowlt.Api.Features.Llm.Models;

namespace Vowlt.Api.Features.Llm.Services;

public interface ILlmService
{
    Task<LlmResponse> GenerateAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default);
}

