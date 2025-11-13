using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Search.DTOs;
using Vowlt.Api.Features.Search.Options;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Hybrid search service combining vector and keyword search with RRF fusion
/// </summary>
public class HybridSearchService(
    IVectorSearchService vectorSearchService,
    IKeywordSearchService keywordSearchService,
    IRankFusionService rankFusionService,
    ICrossEncoderService crossEncoderService,
    VowltDbContext context,
    IOptions<SearchOptions> options,
    ILogger<HybridSearchService> logger) : ISearchService
{
    private readonly SearchOptions _options = options.Value;

    public async Task<Result<SearchResponse>> SearchAsync(
        Guid userId,
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Determine search mode (use default from config if not specified)
            var mode = request.Mode ?? _options.DefaultMode;

            logger.LogInformation(
                "Search started: UserId={UserId}, Query={Query}, Mode={Mode}",
                userId, request.Query, mode);

            // Execute search based on mode
            var response = mode switch
            {
                SearchMode.Vector => await VectorOnlySearchAsync(userId, request, cancellationToken),
                SearchMode.Keyword => await KeywordOnlySearchAsync(userId, request, cancellationToken),
                SearchMode.Hybrid => await HybridSearchAsync(userId, request, cancellationToken),
                _ => throw new ArgumentException($"Unknown search mode: {mode}")
            };

            stopwatch.Stop();
            response = response with { ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds };

            logger.LogInformation(
                "Search completed: UserId={UserId}, Mode={Mode}, Results={ResultCount}, Time={TimeMs}ms",
                userId, mode, response.TotalResults, response.ProcessingTimeMs);

            return Result<SearchResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Search failed: UserId={UserId}, Query={Query}, Time={TimeMs}ms",
                userId, request.Query, stopwatch.Elapsed.TotalMilliseconds);

            return Result<SearchResponse>.Failure("An error occurred during search. Please try again.");
        }
    }

    private async Task<SearchResponse> VectorOnlySearchAsync(
        Guid userId,
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        // Execute vector search
        var vectorResults = await vectorSearchService.SearchAsync(
            userId,
            request.Query,
            request.Limit,
            request.MinimumScore,
            request.FromDate,
            request.ToDate,
            request.Domain,
            cancellationToken);

        // Fetch bookmarks
        var bookmarkIds = vectorResults.Select(r => r.BookmarkId).ToList();
        var bookmarks = await FetchBookmarksAsync(bookmarkIds, cancellationToken);

        // Convert to DTOs
        var results = vectorResults
            .Select(r =>
            {
                var bookmark = bookmarks.GetValueOrDefault(r.BookmarkId);
                if (bookmark == null) return null;

                return new SearchResultDto
                {
                    Id = bookmark.Id,
                    Url = bookmark.Url,
                    Title = bookmark.Title,
                    Description = bookmark.Description,
                    Domain = bookmark.Domain,
                    CreatedAt = bookmark.CreatedAt,
                    SimilarityScore = r.SimilarityScore,
                    VectorScore = r.SimilarityScore,
                    VectorRank = r.Rank
                };
            })
            .Where(r => r != null)
            .Cast<SearchResultDto>()
            .ToList();

        return new SearchResponse
        {
            Query = request.Query,
            Results = results,
            TotalResults = results.Count,
            ProcessingTimeMs = 0,
            Mode = SearchMode.Vector,
            VectorResultCount = results.Count
        };
    }

    private async Task<SearchResponse> KeywordOnlySearchAsync(
        Guid userId,
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        // Execute keyword search
        var keywordResults = await keywordSearchService.SearchAsync(
            userId,
            request.Query,
            request.Limit,
            request.FromDate,
            request.ToDate,
            request.Domain,
            cancellationToken);

        // Fetch bookmarks
        var bookmarkIds = keywordResults.Select(r => r.BookmarkId).ToList();
        var bookmarks = await FetchBookmarksAsync(bookmarkIds, cancellationToken);

        // Convert to DTOs
        var results = keywordResults
            .Select(r =>
            {
                var bookmark = bookmarks.GetValueOrDefault(r.BookmarkId);
                if (bookmark == null) return null;

                return new SearchResultDto
                {
                    Id = bookmark.Id,
                    Url = bookmark.Url,
                    Title = bookmark.Title,
                    Description = bookmark.Description,
                    Domain = bookmark.Domain,
                    CreatedAt = bookmark.CreatedAt,
                    SimilarityScore = r.Bm25Score,
                    KeywordScore = r.Bm25Score,
                    KeywordRank = r.Rank
                };
            })
            .Where(r => r != null)
            .Cast<SearchResultDto>()
            .ToList();

        return new SearchResponse
        {
            Query = request.Query,
            Results = results,
            TotalResults = results.Count,
            ProcessingTimeMs = 0,
            Mode = SearchMode.Keyword,
            KeywordResultCount = results.Count
        };
    }

    private async Task<SearchResponse> HybridSearchAsync(
        Guid userId,
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        // Stage 1: Execute both searches in parallel
        var vectorTask = vectorSearchService.SearchAsync(
            userId,
            request.Query,
            _options.MaxVectorResults,
            request.MinimumScore,
            request.FromDate,
            request.ToDate,
            request.Domain,
            cancellationToken);

        var keywordTask = keywordSearchService.SearchAsync(
            userId,
            request.Query,
            _options.MaxKeywordResults,
            request.FromDate,
            request.ToDate,
            request.Domain,
            cancellationToken);

        await Task.WhenAll(vectorTask, keywordTask);

        var vectorResults = await vectorTask;
        var keywordResults = await keywordTask;

        logger.LogInformation(
            "Hybrid search fetched: Vector={VectorCount}, Keyword={KeywordCount}",
            vectorResults.Count, keywordResults.Count);

        // Fuse results with RRF
        var fusedResults = rankFusionService.FuseResults(
            vectorResults,
            keywordResults,
            _options.RrfK);

        // Log RRF scores before filtering for debugging
        logger.LogInformation(
            "RRF scores before filter (showing top 10): {@Scores}",
            fusedResults.Take(10).Select(r => new
            {
                r.BookmarkId,
                r.RrfScore,
                r.VectorRank,
                r.KeywordRank,
                r.VectorScore,
                r.KeywordScore
            }));

        // Stage 2: Cross-encoder reranking (if enabled)
        List<Models.FusedSearchResult> rerankedResults;

        if (_options.EnableCrossEncoderReranking)
        {
            // Take top N candidates for reranking
            var candidates = fusedResults
                .Where(r => r.RrfScore >= _options.MinimumRrfScore)
                .Take(_options.RerankCandidateLimit)
                .ToList();

            logger.LogInformation(
                "Reranking {Count} candidates with cross-encoder",
                candidates.Count);

            // Fetch bookmarks for candidates to get text for reranking
            var candidateIds = candidates.Select(c => c.BookmarkId).ToList();
            var candidateBookmarks = await FetchBookmarksAsync(candidateIds, cancellationToken);

            // Prepare texts for reranking (combine title + description + url)
            var textsToRerank = candidates
                .Select(c =>
                {
                    var bookmark = candidateBookmarks.GetValueOrDefault(c.BookmarkId);
                    if (bookmark == null) return string.Empty;

                    return $"{bookmark.Title} {bookmark.Description} {bookmark.Url}"
                        .Trim();
                })
                .ToList();

            // Call cross-encoder
            var crossEncoderScores = await crossEncoderService.RerankAsync(
                request.Query,
                textsToRerank,
                cancellationToken);

            // Add cross-encoder scores to results and filter
            rerankedResults = candidates
                .Select((c, index) => c with { CrossEncoderScore = crossEncoderScores[index] })
                .Where(r => r.CrossEncoderScore >= _options.MinimumCrossEncoderScore)
                .OrderByDescending(r => r.CrossEncoderScore)  // Sort by cross-encoder score
                .ToList();

            logger.LogInformation(
                "After cross-encoder filter (>= {MinScore}): {Count} results (filtered out {Filtered})",
                _options.MinimumCrossEncoderScore,
                rerankedResults.Count,
                candidates.Count - rerankedResults.Count);
        }
        else
        {
            // No reranking - just filter by RRF
            rerankedResults = fusedResults
                .Where(r => r.RrfScore >= _options.MinimumRrfScore)
                .ToList();

            logger.LogInformation(
                "After RRF filter (>= {MinScore}): {Count} results (filtered out {Filtered})",
                _options.MinimumRrfScore,
                rerankedResults.Count,
                fusedResults.Count - rerankedResults.Count);
        }

        // Take top N after reranking
        var topResults = rerankedResults.Take(request.Limit).ToList();

        // Fetch bookmarks
        var bookmarkIds = topResults.Select(r => r.BookmarkId).ToList();
        var bookmarks = await FetchBookmarksAsync(bookmarkIds, cancellationToken);

        // Convert to DTOs with full score breakdown
        var results = topResults
            .Select(r =>
            {
                var bookmark = bookmarks.GetValueOrDefault(r.BookmarkId);
                if (bookmark == null) return null;

                return new SearchResultDto
                {
                    Id = bookmark.Id,
                    Url = bookmark.Url,
                    Title = bookmark.Title,
                    Description = bookmark.Description,
                    Domain = bookmark.Domain,
                    CreatedAt = bookmark.CreatedAt,
                    SimilarityScore = r.CrossEncoderScore ?? r.RrfScore,  // Use cross-encoder if available
                    VectorScore = r.VectorScore,
                    KeywordScore = r.KeywordScore,
                    HybridScore = r.RrfScore,
                    VectorRank = r.VectorRank,
                    KeywordRank = r.KeywordRank
                };
            })
            .Where(r => r != null)
            .Cast<SearchResultDto>()
            .ToList();

        return new SearchResponse
        {
            Query = request.Query,
            Results = results,
            TotalResults = results.Count,
            ProcessingTimeMs = 0,
            Mode = SearchMode.Hybrid,
            VectorResultCount = vectorResults.Count,
            KeywordResultCount = keywordResults.Count
        };
    }

    private async Task<Dictionary<Guid, BookmarkData>> FetchBookmarksAsync(
        List<Guid> bookmarkIds,
        CancellationToken cancellationToken)
    {
        if (bookmarkIds.Count == 0)
            return new Dictionary<Guid, BookmarkData>();

        var bookmarks = await context.Bookmarks
            .AsNoTracking()
            .Where(b => bookmarkIds.Contains(b.Id))
            .Select(b => new BookmarkData
            {
                Id = b.Id,
                Url = b.Url,
                Title = b.Title,
                Description = b.Description,
                Domain = b.Domain,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return bookmarks.ToDictionary(b => b.Id);
    }

    public async Task<Result<SearchResponse>> FindSimilarAsync(
      Guid userId,
      Guid bookmarkId,
      int limit = 10,
      CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "FindSimilar started: UserId={UserId}, BookmarkId={BookmarkId}, Limit={Limit}",
                userId, bookmarkId, limit);

            // Get the bookmark's embedding
            var bookmark = await context.Bookmarks
                .AsNoTracking()
                .Where(b => b.Id == bookmarkId && b.UserId == userId && b.Embedding != null)
                .Select(b => new { b.Embedding, b.Title })
                .FirstOrDefaultAsync(cancellationToken);

            if (bookmark == null)
            {
                return Result<SearchResponse>.Failure("Bookmark not found or has no embedding.");
            }

            // Find similar bookmarks using vector similarity
            var similarBookmarks = await context.Bookmarks
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Id != bookmarkId && b.Embedding != null)
                .OrderBy(b => b.Embedding!.CosineDistance(bookmark.Embedding!))
                .Take(limit)
                .Select(b => new
                {
                    b.Id,
                    b.Url,
                    b.Title,
                    b.Description,
                    b.Domain,
                    b.CreatedAt,
                    Distance = b.Embedding!.CosineDistance(bookmark.Embedding!)
                })
                .ToListAsync(cancellationToken);

            // Convert to DTOs
            var results = similarBookmarks.Select(b => new SearchResultDto
            {
                Id = b.Id,
                Url = b.Url,
                Title = b.Title,
                Description = b.Description,
                Domain = b.Domain,
                CreatedAt = b.CreatedAt,
                SimilarityScore = 1.0 - (b.Distance / 2.0),
                VectorScore = 1.0 - (b.Distance / 2.0)
            }).ToList();

            stopwatch.Stop();

            var response = new SearchResponse
            {
                Query = $"Similar to: {bookmark.Title}",
                Results = results,
                TotalResults = results.Count,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                Mode = SearchMode.Vector, // FindSimilar always uses vector search
                VectorResultCount = results.Count
            };

            logger.LogInformation(
                "FindSimilar completed: BookmarkId={BookmarkId}, Results={ResultCount}, Time={TimeMs}ms",
                bookmarkId, response.TotalResults, response.ProcessingTimeMs);

            return Result<SearchResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "FindSimilar failed: UserId={UserId}, BookmarkId={BookmarkId}, Time={TimeMs}ms",
                userId, bookmarkId, stopwatch.Elapsed.TotalMilliseconds);

            return Result<SearchResponse>.Failure("An error occurred while finding similar bookmarks. Please try again.");
        }
    }

    private record BookmarkData
    {
        public required Guid Id { get; init; }
        public required string Url { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public string? Domain { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
