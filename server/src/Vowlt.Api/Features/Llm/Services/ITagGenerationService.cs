namespace Vowlt.Api.Features.Llm.Services;

public interface ITagGenerationService
{
    Task<List<string>> GenerateTagsAsync(
        string title,
        string? description,
        string? notes,
        CancellationToken cancellationToken = default);
}
