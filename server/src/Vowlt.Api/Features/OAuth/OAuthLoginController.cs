
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Vowlt.Api.Features.Auth.Models;

namespace Vowlt.Api.Features.OAuth;

/// <summary>
/// OAuth login endpoint for cookie-based authentication flow.
/// Used by browser extensions and mobile apps during OAuth authorization.
/// </summary>
[ApiController]
[Route("oauth")]
public class OAuthLoginController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<OAuthLoginController> logger) : ControllerBase
{
    /// <summary>
    /// Login endpoint for OAuth flow.
    /// Accepts credentials, sets cookie, and redirects back to authorize endpoint.
    /// </summary>
    /// <remarks>
    /// Flow:
    /// 1. Extension opens /oauth/authorize (requires auth)
    /// 2. User not authenticated → 401 with redirect to /oauth/login
    /// 3. Extension calls this endpoint with credentials
    /// 4. Server sets cookie and redirects back to /oauth/authorize
    /// 5. Authorize endpoint now sees authenticated user
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] OAuthLoginRequest request)
    {
        logger.LogInformation("OAuth login attempt for email: {Email}", request.Email);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "invalid_request", error_description = "Email and password are required" });
        }

        // Find user
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            logger.LogWarning("OAuth login failed: User not found - {Email}", request.Email);
            return Unauthorized(new { error = "invalid_credentials", error_description = "Invalid email or password" });
        }

        // Verify password
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            logger.LogWarning("OAuth login failed: Invalid password for {Email}. Locked: {Locked}",
                request.Email, result.IsLockedOut);

            if (result.IsLockedOut)
            {
                return Unauthorized(new { error = "account_locked", error_description = "Account is locked due to multiple failed login attempts" });
            }

            return Unauthorized(new { error = "invalid_credentials", error_description = "Invalid email or password" });
        }

        // Sign in with cookie (persistent cookie for OAuth flow)
        await signInManager.SignInAsync(user, isPersistent: true, authenticationMethod: IdentityConstants.ApplicationScheme);

        logger.LogInformation("OAuth login successful for {Email}", request.Email);

        // Return success - extension will retry authorize endpoint
        return Ok(new
        {
            success = true,
            message = "Login successful. You can now complete the OAuth flow."
        });
    }

    /// <summary>
    /// Logout endpoint for OAuth flow (clears cookie).
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { success = true });
    }
}

/// <summary>
/// OAuth login request.
/// </summary>
public record OAuthLoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
