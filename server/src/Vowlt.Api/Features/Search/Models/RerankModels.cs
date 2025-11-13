namespace Vowlt.Api.Features.Search.Models;

/// <summary>
/// Request model for cross-encoder reranking
/// </summary>
public record RerankRequest
{
    public required string Query { get; init; }
    public required List<string> Texts { get; init; }
}

/// <summary>
/// Response model for cross-encoder reranking
/// </summary>
public record RerankResponse
{
    public required List<RerankItem> Scores { get; init; }
}

/// <summary>
/// Individual rerank result
/// </summary>
public record RerankItem
{
    public required int Index { get; init; }
    public required double Score { get; init; }
}

