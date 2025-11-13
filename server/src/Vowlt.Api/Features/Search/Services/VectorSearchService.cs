using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Embedding.Services;
using Vowlt.Api.Features.Search.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Vector (semantic) search implementation using pgvector
/// </summary>
public class VectorSearchService(
    VowltDbContext context,
    IEmbeddingService embeddingService,
    ILogger<VectorSearchService> logger) : IVectorSearchService
{
    public async Task<List<VectorSearchResult>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        double minimumScore = 0.5,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? domain = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Vector search: UserId={UserId}, Query={Query}, Limit={Limit}, MinScore={MinScore}",
                userId, query, limit, minimumScore);

            // 1. Generate embedding for search query
            var queryEmbeddingArray = await embeddingService.EmbedTextAsync(query, cancellationToken);
            var queryEmbedding = new Vector(queryEmbeddingArray);

            // 2. Build base query with multi-tenant filtering
            var baseQuery = context.Bookmarks
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Embedding != null);

            // 3. Apply optional filters
            if (fromDate.HasValue)
                baseQuery = baseQuery.Where(b => b.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                baseQuery = baseQuery.Where(b => b.CreatedAt <= toDate.Value);

            if (!string.IsNullOrWhiteSpace(domain))
                baseQuery = baseQuery.Where(b => b.Domain == domain);

            // 4. Perform vector similarity search
            var maxDistance = SearchConstants.CosineDistanceNormalizationFactor * (1 - minimumScore);

            var results = await baseQuery
                .Where(b => b.Embedding!.CosineDistance(queryEmbedding) <= maxDistance)
                .OrderBy(b => b.Embedding!.CosineDistance(queryEmbedding))
                .Take(limit)
                .Select(b => new
                {
                    b.Id,
                    Distance = b.Embedding!.CosineDistance(queryEmbedding)
                })
                .ToListAsync(cancellationToken);

            // 5. Convert to VectorSearchResult with ranks
            var vectorResults = results.Select((r, index) => new VectorSearchResult
            {
                BookmarkId = r.Id,
                Distance = r.Distance,
                SimilarityScore = CalculateSimilarityScore(r.Distance),
                Rank = index + 1 // Rank starts at 1
            }).ToList();

            logger.LogInformation(
                "Vector search completed: Found {Count} results",
                vectorResults.Count);

            return vectorResults;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during vector search: UserId={UserId}, Query={Query}",
                userId, query);
            throw;
        }
    }

    private static double CalculateSimilarityScore(double cosineDistance)
    {
        return 1.0 - (cosineDistance / SearchConstants.CosineDistanceNormalizationFactor);
    }
}
