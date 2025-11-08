using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Bookmarks.Services;
using Vowlt.Api.Features.Search.DTOs;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Search.Services;

public class SearchService(
    VowltDbContext context,
    IEmbeddingService embeddingService,
    ILogger<SearchService> logger) : ISearchService
{
    public async Task<Result<SearchResponse>> SearchAsync(
        Guid userId,
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Generate embedding for search query
            logger.LogInformation(
                "Generating embedding for search query: {Query}",
                request.Query);

            float[] queryEmbeddingArray;
            try
            {
                queryEmbeddingArray = await embeddingService.EmbedTextAsync(
                    request.Query,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate embedding for search query");
                return Result<SearchResponse>.Failure(
                    "Failed to process search query. Please try again.");
            }

            var queryEmbedding = new Vector(queryEmbeddingArray);

            // 2. Build base query with multi-tenant filtering
            var query = context.Bookmarks
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Embedding != null);

            // 3. Apply optional filters
            if (request.FromDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt <= request.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Domain))
            {
                query = query.Where(b => b.Domain == request.Domain);
            }

            // 4. Perform vector similarity search using pgvector
            // CosineDistance returns 0 for identical vectors, 2 for opposite
            // We convert to similarity score: 1 - (distance / 2) = range [0, 1]
            var results = await query
                .Select(b => new
                {
                    Bookmark = b,
                    Distance = b.Embedding!.CosineDistance(queryEmbedding),
                    Score = 1 - (b.Embedding!.CosineDistance(queryEmbedding) / 2)
                })
                .Where(x => x.Score >= request.MinimumScore)
                .OrderByDescending(x => x.Score)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();

            logger.LogInformation(
                "Search completed in {ElapsedMs}ms. Found {Count} results for query: {Query}",
                stopwatch.ElapsedMilliseconds,
                results.Count,
                request.Query);

            // 5. Map to response DTOs
            var searchResults = results.Select(r => new SearchResultDto
            {
                Id = r.Bookmark.Id,
                Url = r.Bookmark.Url,
                Title = r.Bookmark.Title,
                Description = r.Bookmark.Description,
                Domain = r.Bookmark.Domain,
                CreatedAt = r.Bookmark.CreatedAt,
                SimilarityScore = Math.Round(r.Score, 4)  // Round to 4 decimal places
            });

            var response = new SearchResponse
            {
                Query = request.Query,
                Results = searchResults,
                TotalResults = results.Count,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };

            return Result<SearchResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Unexpected error during search");
            return Result<SearchResponse>.Failure("An error occurred during search");
        }
    }

    public async Task<Result<SearchResponse>> FindSimilarAsync(
        Guid userId,
        Guid bookmarkId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Get the source bookmark
        var sourceBookmark = await context.Bookmarks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.UserId == userId,
                cancellationToken);

        if (sourceBookmark == null)
        {
            return Result<SearchResponse>.Failure("Bookmark not found");
        }

        if (sourceBookmark.Embedding == null)
        {
            return Result<SearchResponse>.Failure(
                "Source bookmark does not have an embedding");
        }

        logger.LogInformation(
            "Finding similar bookmarks to: {Title} (ID: {Id})",
            sourceBookmark.Title,
            bookmarkId);

        // 2. Find similar bookmarks using the source bookmark's embedding
        var results = await context.Bookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId
                && b.Id != bookmarkId  // Exclude the source bookmark
                && b.Embedding != null)
            .Select(b => new
            {
                Bookmark = b,
                Distance = b.Embedding!.CosineDistance(sourceBookmark.Embedding),
                Score = 1 - (b.Embedding!.CosineDistance(sourceBookmark.Embedding) / 2)
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToListAsync(cancellationToken);

        stopwatch.Stop();

        logger.LogInformation(
            "Found {Count} similar bookmarks in {ElapsedMs}ms",
            results.Count,
            stopwatch.ElapsedMilliseconds);

        // 3. Map to response
        var searchResults = results.Select(r => new SearchResultDto
        {
            Id = r.Bookmark.Id,
            Url = r.Bookmark.Url,
            Title = r.Bookmark.Title,
            Description = r.Bookmark.Description,
            Domain = r.Bookmark.Domain,
            CreatedAt = r.Bookmark.CreatedAt,
            SimilarityScore = Math.Round(r.Score, 4)
        });

        var response = new SearchResponse
        {
            Query = $"Similar to: {sourceBookmark.Title}",
            Results = searchResults,
            TotalResults = results.Count,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        };

        return Result<SearchResponse>.Success(response);
    }
}
