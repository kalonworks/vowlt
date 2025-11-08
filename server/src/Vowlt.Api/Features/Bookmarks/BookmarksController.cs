using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vowlt.Api.Features.Bookmarks.DTOs;
using Vowlt.Api.Features.Bookmarks.Services;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Bookmarks;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookmarksController(IBookmarkService bookmarkService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("expensive-operation")]
    [ProducesResponseType(typeof(BookmarkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookmarkDto>> CreateBookmark(
        [FromBody] CreateBookmarkRequest request)
    {
        var userId = GetUserId();
        var result = await bookmarkService.CreateBookmarkAsync(userId, request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error, errors = result.Errors });
        }

        return CreatedAtAction(
            nameof(GetBookmark),
            new { id = result.Value!.Id },
            result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookmarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookmarkDto>> GetBookmark(Guid id)
    {
        var userId = GetUserId();
        var result = await bookmarkService.GetBookmarkByIdAsync(userId, id);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        if (result.Value == null)
        {
            return NotFound(new { error = "Bookmark not found" });
        }

        return result.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookmarkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetBookmarks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var userId = GetUserId();
        var result = await bookmarkService.GetUserBookmarksAsync(
            userId,
            pageNumber,
            pageSize,
            search);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("by-url")]
    [ProducesResponseType(typeof(BookmarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookmarkDto>> GetBookmarkByUrl([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new { error = "URL is required" });
        }

        var userId = GetUserId();
        var result = await bookmarkService.GetBookmarkByUrlAsync(userId, url);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        if (result.Value == null)
        {
            return NotFound(new { error = "Bookmark not found" });
        }

        return result.Value;
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BookmarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookmarkDto>> UpdateBookmark(
        Guid id,
        [FromBody] UpdateBookmarkRequest request)
    {
        var userId = GetUserId();
        var result = await bookmarkService.UpdateBookmarkAsync(userId, id, request);

        if (!result.IsSuccess)
        {
            return result.Error == "Bookmark not found"
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return result.Value!;
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteBookmark(Guid id)
    {
        var userId = GetUserId();
        var result = await bookmarkService.DeleteBookmarkAsync(userId, id);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeleteAllBookmarks()
    {
        var userId = GetUserId();
        var result = await bookmarkService.DeleteAllUserBookmarksAsync(userId);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = $"Deleted {result.Value} bookmarks", count = result.Value });
    }

    [HttpPost("{id}/accessed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsAccessed(Guid id)
    {
        var userId = GetUserId();
        var result = await bookmarkService.MarkAsAccessedAsync(userId, id);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(new { message = "Bookmark marked as accessed" });
    }

    [HttpPut("{id}/metadata")]
    [ProducesResponseType(typeof(BookmarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookmarkDto>> UpdateMetadata(
        Guid id,
        [FromBody] UpdateMetadataRequest request)
    {
        var userId = GetUserId();
        var result = await bookmarkService.UpdateMetadataAsync(userId, id, request);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return result.Value!;
    }

    [HttpPost("{id}/regenerate-embedding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegenerateEmbedding(Guid id)
    {
        var userId = GetUserId();
        var result = await bookmarkService.RegenerateEmbeddingAsync(userId, id);

        if (!result.IsSuccess)
        {
            return result.Error == "Bookmark not found"
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(new { message = "Embedding regenerated successfully" });
    }

    // Helper method to extract userId from JWT claims
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
}
