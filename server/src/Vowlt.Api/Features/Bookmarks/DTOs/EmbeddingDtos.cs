namespace Vowlt.Api.Features.Bookmarks.DTOs;

public record EmbedRequest(string[] Texts);

public record EmbedResponse(
    float[][] Embeddings,
    string Model,
    int Dimensions,
    float ProcessingTimeMs);
