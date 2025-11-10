using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Bookmarks.DTOs;
using Vowlt.Api.Features.Bookmarks.Models;
using Vowlt.Api.Features.Embedding.Options;
using Vowlt.Api.Features.Embedding.Services;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Bookmarks.Services;

public class BookmarkService(
    VowltDbContext context,
    IEmbeddingService embeddingService,
    IOptions<EmbeddingOptions> embeddingOptions,
    TimeProvider timeProvider,
    ILogger<BookmarkService> logger) : IBookmarkService
{
    private readonly EmbeddingOptions _embeddingOptions = embeddingOptions.Value;
    public async Task<Result<BookmarkDto>> CreateBookmarkAsync(
        Guid userId,
        CreateBookmarkRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Check if bookmark with this URL already exists for this user
        var existingBookmark = await context.Bookmarks
            .FirstOrDefaultAsync(
                b => b.UserId == userId && b.Url == request.Url,
                cancellationToken);

        if (existingBookmark != null)
        {
            logger.LogWarning(
                "User {UserId} attempted to create duplicate bookmark for URL: {Url}",
                userId, request.Url);
            return Result<BookmarkDto>.Failure("A bookmark with this URL already exists");
        }

        // 2. Create bookmark entity using factory method
        var bookmark = Bookmark.Create(
            userId,
            request.Url,
            request.Title,
            timeProvider.GetUtcNow().UtcDateTime,
            request.Description,
            request.Notes);

        // 3. Set optional full text if provided
        if (!string.IsNullOrWhiteSpace(request.FullText))
        {
            bookmark.SetFullText(request.FullText, timeProvider.GetUtcNow().UtcDateTime);
        }

        // Set user tags if provided
        if (request.Tags is not null && request.Tags.Count > 0)
        {
            bookmark.SetTags(request.Tags, timeProvider.GetUtcNow().UtcDateTime);
        }

        // 4. Generate embedding (CRITICAL: This is synchronous - fails if embedding service is down)
        try
        {
            var textForEmbedding = bookmark.GetTextForEmbedding();
            logger.LogInformation(
                "Generating embedding for bookmark {BookmarkId}. Text length: {Length}",
                bookmark.Id, textForEmbedding.Length);

            var embedding = await embeddingService.EmbedTextAsync(
                textForEmbedding,
                cancellationToken);

            bookmark.SetEmbedding(embedding, _embeddingOptions.VectorDimensions);
            logger.LogInformation(
                "Successfully generated embedding for bookmark {BookmarkId}",
                bookmark.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to generate embedding for bookmark. Bookmark creation aborted.");
            return Result<BookmarkDto>.Failure(
                "Failed to generate embedding. Please try again later.");
        }

        // 5. Save to database
        context.Bookmarks.Add(bookmark);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created bookmark {BookmarkId} for user {UserId}",
            bookmark.Id, userId);

        return Result<BookmarkDto>.Success(MapToDto(bookmark));
    }

    public async Task<Result<BookmarkDto>> GetBookmarkByIdAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default)
    {
        var bookmark = await context.Bookmarks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.UserId == userId,
                cancellationToken);

        if (bookmark == null)
        {
            logger.LogWarning(
                "Bookmark {BookmarkId} not found for user {UserId}",
                bookmarkId, userId);
            return Result<BookmarkDto>.Failure("Bookmark not found");
        }

        return Result<BookmarkDto>.Success(MapToDto(bookmark));
    }

    public async Task<Result<PagedResult<BookmarkDto>>> GetUserBookmarksAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        string? searchQuery = null,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1 || pageSize > 100)
            pageSize = 20;

        // Base query - always filter by userId (multi-tenancy)
        var query = context.Bookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId);

        // Apply search filter if provided (simple text search)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchLower = searchQuery.ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(searchLower) ||
                (b.Description != null && b.Description.ToLower().Contains(searchLower)) ||
                (b.Notes != null && b.Notes.ToLower().Contains(searchLower)) ||
                (b.Domain != null && b.Domain.ToLower().Contains(searchLower)));
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated results, ordered by most recent first
        var bookmarks = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Retrieved {Count} bookmarks for user {UserId} (page {Page}/{TotalPages})",
            bookmarks.Count, userId, pageNumber, (totalCount + pageSize - 1) / pageSize);

        var pagedResult = new PagedResult<BookmarkDto>
        {
            Items = bookmarks.Select(MapToDto),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Result<PagedResult<BookmarkDto>>.Success(pagedResult);
    }

    public async Task<Result<BookmarkDto>> GetBookmarkByUrlAsync(
        Guid userId,
        string url,
        CancellationToken cancellationToken = default)
    {
        var bookmark = await context.Bookmarks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.UserId == userId && b.Url == url,
                cancellationToken);

        if (bookmark == null)
        {
            return Result<BookmarkDto>.Failure("Bookmark not found");
        }

        return Result<BookmarkDto>.Success(MapToDto(bookmark));

    }

    public async Task<Result<BookmarkDto>> UpdateBookmarkAsync(
        Guid userId,
        Guid bookmarkId,
        UpdateBookmarkRequest request,
        CancellationToken cancellationToken = default)
    {
        // Find bookmark (with tracking for update)
        var bookmark = await context.Bookmarks
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.UserId == userId,
                cancellationToken);

        if (bookmark == null)
        {
            logger.LogWarning(
                "Bookmark {BookmarkId} not found for user {UserId}",
                bookmarkId, userId);
            return Result<BookmarkDto>.Failure("Bookmark not found");
        }

        // Update content using entity's Update method
        bookmark.Update(
            timeProvider.GetUtcNow().UtcDateTime,
            request.Title,
            request.Description,
            request.Notes);

        // Update full text if provided
        if (!string.IsNullOrWhiteSpace(request.FullText))
        {
            bookmark.SetFullText(request.FullText, timeProvider.GetUtcNow().UtcDateTime);
        }

        // Update user tags if provided
        if (request.Tags is not null)
        {
            bookmark.SetTags(request.Tags, timeProvider.GetUtcNow().UtcDateTime);
        }

        // Regenerate embedding (content changed)
        try
        {
            var textForEmbedding = bookmark.GetTextForEmbedding();
            logger.LogInformation(
                "Regenerating embedding for bookmark {BookmarkId} after update",
                bookmark.Id);

            var embedding = await embeddingService.EmbedTextAsync(
                textForEmbedding,
                cancellationToken);

            bookmark.SetEmbedding(embedding, _embeddingOptions.VectorDimensions);
            logger.LogInformation(
                "Successfully regenerated embedding for bookmark {BookmarkId}",
                bookmark.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to regenerate embedding for bookmark {BookmarkId}. Update aborted.",
                bookmark.Id);
            return Result<BookmarkDto>.Failure(
                "Failed to regenerate embedding. Update aborted.");
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated bookmark {BookmarkId} for user {UserId}",
            bookmark.Id, userId);

        return Result<BookmarkDto>.Success(MapToDto(bookmark));
    }

    public async Task<Result<bool>> DeleteBookmarkAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default)
    {
        var bookmark = await context.Bookmarks
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.UserId == userId,
                cancellationToken);

        if (bookmark == null)
        {
            logger.LogWarning(
                "Bookmark {BookmarkId} not found for user {UserId}",
                bookmarkId, userId);
            return Result<bool>.Failure("Bookmark not found");
        }

        context.Bookmarks.Remove(bookmark);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted bookmark {BookmarkId} for user {UserId}",
            bookmarkId, userId);

        return Result<bool>.Success(true);
    }

    public async Task<Result<int>> DeleteAllUserBookmarksAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var count = await context.Bookmarks
            .Where(b => b.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogWarning(
            "Deleted all {Count} bookmarks for user {UserId}",
            count, userId);

        return Result<int>.Success(count);
    }


    public async Task<Result<bool>> RegenerateEmbeddingAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default)
    {
        var bookmark = await context.Bookmarks
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.UserId == userId,
                cancellationToken);

        if (bookmark == null)
        {
            return Result<bool>.Failure("Bookmark not found");
        }

        try
        {
            var textForEmbedding = bookmark.GetTextForEmbedding();
            var embedding = await embeddingService.EmbedTextAsync(
                textForEmbedding,
                cancellationToken);

            bookmark.SetEmbedding(embedding, _embeddingOptions.VectorDimensions);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Regenerated embedding for bookmark {BookmarkId}",
                bookmark.Id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to regenerate embedding for bookmark {BookmarkId}",
                bookmark.Id);
            return Result<bool>.Failure("Failed to regenerate embedding");
        }
    }

    public async Task<Result<BookmarkDto>> UpdateMetadataAsync(
        Guid userId,
        Guid bookmarkId,
        UpdateMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        var bookmark = await context.Bookmarks
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.UserId == userId,
                cancellationToken);

        if (bookmark == null)
        {
            return Result<BookmarkDto>.Failure("Bookmark not found");
        }

        bookmark.SetMetadata(request.FaviconUrl, request.OgImageUrl, timeProvider.GetUtcNow().UtcDateTime);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated metadata for bookmark {BookmarkId}",
            bookmark.Id);

        return Result<BookmarkDto>.Success(MapToDto(bookmark));
    }

    public async Task<Result<bool>> MarkAsAccessedAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default)
    {
        var bookmark = await context.Bookmarks
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.UserId == userId,
                cancellationToken);

        if (bookmark == null)
        {
            return Result<bool>.Failure("Bookmark not found");
        }

        bookmark.MarkAsAccessed(timeProvider.GetUtcNow().UtcDateTime);
        await context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    // Private helper method to map entity to DTO
    private static BookmarkDto MapToDto(Bookmark bookmark)
    {
        return new BookmarkDto
        {
            Id = bookmark.Id,
            Url = bookmark.Url,
            Title = bookmark.Title,
            Description = bookmark.Description,
            Notes = bookmark.Notes,
            FullText = bookmark.FullText,
            FaviconUrl = bookmark.FaviconUrl,
            OgImageUrl = bookmark.OgImageUrl,
            Domain = bookmark.Domain,
            CreatedAt = bookmark.CreatedAt,
            UpdatedAt = bookmark.UpdatedAt,
            LastAccessedAt = bookmark.LastAccessedAt,
            Tags = bookmark.Tags,
            GeneratedTags = bookmark.GeneratedTags,
            HasEmbedding = bookmark.Embedding != null
        };
    }
}
