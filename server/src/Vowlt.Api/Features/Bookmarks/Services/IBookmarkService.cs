using Vowlt.Api.Features.Bookmarks.DTOs;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Bookmarks.Services;

public interface IBookmarkService
{
    Task<Result<BookmarkDto>> CreateBookmarkAsync(
        Guid userId,
        CreateBookmarkRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<BookmarkDto>> GetBookmarkByIdAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<BookmarkDto>>> GetUserBookmarksAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        string? searchQuery = null,
        CancellationToken cancellationToken = default);

    Task<Result<BookmarkDto>> GetBookmarkByUrlAsync(
        Guid userId,
        string url,
        CancellationToken cancellationToken = default);

    Task<Result<BookmarkDto>> UpdateBookmarkAsync(
        Guid userId,
        Guid bookmarkId,
        UpdateBookmarkRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteBookmarkAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default);

    Task<Result<int>> DeleteAllUserBookmarksAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RegenerateEmbeddingAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default);

    Task<Result<BookmarkDto>> UpdateMetadataAsync(
        Guid userId,
        Guid bookmarkId,
        UpdateMetadataRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> MarkAsAccessedAsync(
        Guid userId,
        Guid bookmarkId,
        CancellationToken cancellationToken = default);
}
