using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.OAuth.DTOs;
using Vowlt.Api.Features.OAuth.Services;

namespace Vowlt.Api.Features.OAuth;

/// <summary>
/// OAuth 2.1 authorization server endpoints.
/// Implements authorization code flow with PKCE for browser extension and future mobile apps.
/// </summary>
[ApiController]
[Route("oauth")]
public class OAuthController(
    OAuthService oauthService,
    UserManager<ApplicationUser> userManager,
    ILogger<OAuthController> logger) : ControllerBase
{
    /// <summary>
    /// OAuth authorization endpoint (step 1 of the flow).
    /// User must be authenticated. Creates an authorization code and redirects back to the client.
    /// </summary>
    /// <remarks>
    /// Flow:
    /// 1. Extension generates code_verifier and code_challenge (PKCE)
    /// 2. Extension opens this URL in browser with chrome.identity.launchWebAuthFlow
    /// 3. User is already logged in (JWT cookie/session)
    /// 4. Server validates client and redirect URI
    /// 5. Server creates authorization code with code_challenge
    /// 6. Server redirects to redirect_uri with code and state
    /// 7. Extension extracts code from redirect URL
    /// </remarks>

    /// <summary>
    /// OAuth authorization endpoint (step 1 of the flow).
    /// User must be authenticated. Creates an authorization code and redirects back to the client.
    /// </summary>
    [HttpGet("authorize")]
    [Authorize(AuthenticationSchemes = "Identity.Application")]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request)
    {
        logger.LogInformation("=== AUTHORIZE START === User: {User}, ClientId: {ClientId}",
            User?.Identity?.Name ?? "NULL",
            request.ClientId ?? "NULL");

        // Validate code challenge method
        logger.LogInformation("Step 1: Validating code challenge method");
        if (request.CodeChallengeMethod != "S256")
        {
            logger.LogWarning("Invalid code_challenge_method: {Method}", request.CodeChallengeMethod);
            return BadRequest(new { error = "invalid_request", error_description = "code_challenge_method must be S256" });
        }

        // Validate client and redirect URI
        logger.LogInformation("Step 2: Validating client");
        var isValid = await oauthService.ValidateClientAndRedirectUriAsync(request.ClientId, request.RedirectUri);
        if (!isValid)
        {
            logger.LogWarning("Invalid client_id or redirect_uri");
            return BadRequest(new { error = "invalid_client", error_description = "Invalid client_id or redirect_uri" });
        }

        // Get current authenticated user
        logger.LogInformation("Step 3: Getting user. User.Identity.IsAuthenticated={IsAuth}, Name={Name}",
            User?.Identity?.IsAuthenticated, User?.Identity?.Name);
        var user = await userManager.GetUserAsync(User);
        logger.LogInformation("Step 4: User result: {UserFound}", user != null);

        if (user == null)
        {
            logger.LogWarning("User not found despite [Authorize] attribute");
            return Unauthorized(new { error = "unauthorized", error_description = "User not authenticated" });
        }

        logger.LogInformation("Step 5: Creating auth code for user {UserId}", user.Id);
        var authCode = await oauthService.CreateAuthorizationCodeAsync(
            userId: user.Id,
            userEmail: user.Email!,
            userDisplayName: user.DisplayName ?? user.Email!, // NEW: Pass displayName
            clientId: request.ClientId,
            redirectUri: request.RedirectUri,
            codeChallenge: request.CodeChallenge,
            codeChallengeMethod: request.CodeChallengeMethod,
            state: request.State);

        logger.LogInformation("Step 6: Redirecting to {RedirectUri} with code", request.RedirectUri);
        var redirectUrl = $"{request.RedirectUri}?code={authCode.Code}";
        if (!string.IsNullOrEmpty(request.State))
        {
            redirectUrl += $"&state={request.State}";
        }

        return Redirect(redirectUrl);
    }

    /// <summary>
    /// OAuth token endpoint (step 2 of the flow).
    /// Exchanges authorization code for access and refresh tokens.
    /// Validates PKCE code_verifier against code_challenge.
    /// </summary>
    /// <remarks>
    /// Flow:
    /// 1. Extension receives authorization code from redirect
    /// 2. Extension calls this endpoint with code and code_verifier
    /// 3. Server validates code, code_verifier, client_id, and redirect_uri
    /// 4. Server returns access_token and refresh_token
    /// 5. Extension stores tokens in chrome.storage.sync
    /// </remarks>
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest request)
    {
        // Validate grant type
        if (request.GrantType != "authorization_code")
        {
            logger.LogWarning("Invalid grant_type: {GrantType}", request.GrantType);
            return BadRequest(new
            {
                error = "unsupported_grant_type",
                error_description = "Only authorization_code grant type is supported"
            });
        }

        // Validate client and redirect URI first (before attempting token exchange)
        var isClientValid = await oauthService.ValidateClientAndRedirectUriAsync(
            request.ClientId, request.RedirectUri);

        if (!isClientValid)
        {
            logger.LogWarning("Invalid client_id or redirect_uri in token request. ClientId: {ClientId}",
                request.ClientId);
            return BadRequest(new
            {
                error = "invalid_client",
                error_description = "Invalid client_id or redirect_uri"
            });
        }

        // Exchange authorization code for tokens (includes PKCE validation)
        var result = await oauthService.ExchangeCodeForTokensAsync(
            code: request.Code,
            codeVerifier: request.CodeVerifier,
            clientId: request.ClientId,
            redirectUri: request.RedirectUri);

        if (result == null)
        {
            logger.LogWarning("Failed to exchange code for tokens. Code may be invalid, expired, or PKCE validation failed.");
            return BadRequest(new
            {
                error = "invalid_grant",
                error_description = "Invalid authorization code or code_verifier"
            });
        }

        var (accessToken, refreshToken, expiresAt) = result.Value;

        // Calculate expires_in (seconds until expiration)
        var expiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;

        logger.LogInformation("Successfully exchanged authorization code for tokens. ClientId: {ClientId}",
            request.ClientId);

        // Return tokens
        var response = new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            RefreshToken = refreshToken
        };

        return Ok(response);
    }
}
