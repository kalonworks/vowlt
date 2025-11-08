using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vowlt.Api.Features.Auth.DTOs;
using Vowlt.Api.Features.Auth.Services;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request, GetIpAddress());

        if (!result.IsSuccess)
        {
            return BadRequest(ErrorResponse.FromResult(result));
        }

        return result.Value!;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request, GetIpAddress());

        if (!result.IsSuccess)
        {
            return Unauthorized(ErrorResponse.FromResult(result));
        }

        return result.Value!;
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, GetIpAddress());

        if (!result.IsSuccess)
        {
            return Unauthorized(ErrorResponse.FromResult(result));
        }

        return result.Value!;
    }

    [HttpPost("logout")]
    [Authorize]
    [DisableRateLimiting]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await authService.RevokeAllUserTokensAsync(userId, GetIpAddress());

        return new { message = "Logged out successfully" };
    }


    private string? GetIpAddress()
    {
        if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
