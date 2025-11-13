using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vowlt.Api.Features.Auth.DTOs;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.Auth.Services;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    /// <summary>
    /// Register a new user account. Sets authentication cookie for immediate OAuth flow.
    /// Does NOT return tokens - frontend must redirect to OAuth authorize endpoint.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<object>> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(ErrorResponse.FromResult(result));
        }

        // Fetch the actual user from database (has SecurityStamp and all Identity fields)
        var user = await signInManager.UserManager.FindByIdAsync(result.Value!.Id.ToString());

        if (user == null)
        {
            return BadRequest(new { error = "User created but not found" });
        }

        // Sign in user (sets cookie for OAuth flow)
        await signInManager.SignInAsync(user, isPersistent: true);

        return Ok(new
        {
            success = true,
            message = "Account created successfully. Redirecting to OAuth...",
            user = result.Value
        });
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
