using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vowlt.Api.Features.Search.DTOs;
using Vowlt.Api.Features.Search.Services;
using Vowlt.Api.Shared.Controllers;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Search;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController(ISearchService searchService) : VowltControllerBase
{
    [HttpPost]
    [EnableRateLimiting("expensive-operation")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchResponse>> Search(
        [FromBody] SearchRequest request)
    {
        var userId = GetUserId();
        var result = await searchService.SearchAsync(userId, request);

        if (!result.IsSuccess)
        {
            return BadRequest(ErrorResponse.FromResult(result));
        }

        return result.Value!;
    }

    [HttpGet("similar/{bookmarkId}")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchResponse>> FindSimilar(
        Guid bookmarkId,
        [FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var result = await searchService.FindSimilarAsync(userId, bookmarkId, limit);

        if (!result.IsSuccess)
        {
            return result.Error == "Bookmark not found"
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return result.Value!;
    }
}
