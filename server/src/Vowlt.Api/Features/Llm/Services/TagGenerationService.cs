using Vowlt.Api.Features.Llm.Models;

namespace Vowlt.Api.Features.Llm.Services;

public class TagGenerationService(
    ILlmService llmService,
    ILogger<TagGenerationService> logger) : ITagGenerationService
{
    public async Task<List<string>> GenerateTagsAsync(
        string title,
        string? description,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build context from available fields
            var context = BuildContext(title, description, notes);

            // Create the prompt for tag generation
            var prompt = $"""
                  Analyze this bookmark and generate 3-5 relevant tags.

                  Title: {title}
                  {(description != null ? $"Description: {description}" : "")}
                  {(notes != null ? $"Notes: {notes}" : "")}

                  Rules:
                  - Generate 3-5 short, relevant tags
                  - Tags should be lowercase, single words or short phrases (2-3 words max)
                  - Focus on topics, technologies, categories, or themes
                  - Return ONLY the tags as a comma-separated list
                  - No explanations, no numbering, just tags

                  Example output: javascript, web development, tutorial, react, frontend

                  Tags:
                  """;

            var request = new LlmRequest(
                Prompt: prompt,
                Temperature: 0.3, // Lower temperature for more consistent results
                MaxTokens: 50     // Short response - just tags
            );

            var response = await llmService.GenerateAsync(request, cancellationToken);

            if (!response.IsSuccess)
            {
                logger.LogWarning(
                    "Failed to generate tags: {Error}",
                    response.ErrorMessage);
                return [];
            }

            // Parse the comma-separated tags
            var tags = response.Text
                .Split(',')
                .Select(tag => tag.Trim().ToLowerInvariant())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Take(5) // Limit to 5 tags max
                .ToList();

            logger.LogInformation(
                "Generated {Count} tags for bookmark '{Title}'",
                tags.Count,
                title);

            return tags;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error generating tags for bookmark '{Title}'",
                title);
            return [];
        }
    }

    private static string BuildContext(string title, string? description, string? notes)
    {
        var parts = new List<string> { title };

        if (!string.IsNullOrWhiteSpace(description))
            parts.Add(description);

        if (!string.IsNullOrWhiteSpace(notes))
            parts.Add(notes);

        return string.Join(" ", parts);
    }
}
