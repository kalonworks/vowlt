using Microsoft.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Auth.Services;
using Vowlt.Api.Features.OAuth.Models;

namespace Vowlt.Api.Features.OAuth.Services;

/// <summary>
/// Orchestrates OAuth 2.1 authorization code flow with PKCE.
/// </summary>
public class OAuthService(
      VowltDbContext context,
      PKCEValidator pkceValidator,
      IJwtTokenGenerator jwtTokenGenerator,
      IRefreshTokenService refreshTokenService,
      TimeProvider timeProvider,
      ILogger<OAuthService> logger)

{
    /// <summary>
    /// Creates an authorization code for a user.
    /// </summary>

    public async Task<AuthorizationCode> CreateAuthorizationCodeAsync(
        Guid userId,
        string userEmail, // NEW: Accept email parameter
        string clientId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod,
        string? state,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var authCode = AuthorizationCode.Create(
            userId,
            userEmail,
            clientId,
            redirectUri,
            codeChallenge,
            codeChallengeMethod,
            state,
            now);

        context.AuthorizationCodes.Add(authCode);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created authorization code for user {UserId}, client {ClientId}",
            userId, clientId);

        return authCode;
    }


    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens.
    /// Validates PKCE code_verifier.
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)?>
      ExchangeCodeForTokensAsync(
          string code,
          string codeVerifier,
          string clientId,
          string redirectUri,
          CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Find authorization code (NO .Include - we have email denormalized)
        var authCode = await context.AuthorizationCodes
            .FirstOrDefaultAsync(ac => ac.Code == code && ac.ClientId == clientId,
                cancellationToken);

        if (authCode == null)
        {
            logger.LogWarning("Authorization code not found or client_id mismatch");
            return null;
        }

        // Validate code is still valid (not expired or used)
        if (!authCode.IsValid(now))
        {
            logger.LogWarning("Authorization code is expired or already used");
            return null;
        }

        // Validate redirect URI matches
        if (authCode.RedirectUri != redirectUri)
        {
            logger.LogWarning("Redirect URI mismatch");
            return null;
        }

        // Validate PKCE code_verifier (CRITICAL SECURITY CHECK)
        if (!pkceValidator.ValidateCodeVerifier(
            codeVerifier,
            authCode.CodeChallenge,
            authCode.CodeChallengeMethod))
        {
            logger.LogWarning("PKCE validation failed for authorization code");
            return null;
        }

        // Mark code as used (one-time use only)
        authCode.MarkAsUsed(now);
        await context.SaveChangesAsync(cancellationToken);

        // Get client to determine token lifetimes
        var client = await context.OAuthClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);

        if (client == null)
        {
            logger.LogError("Client not found despite earlier validation");
            return null;
        }

        // Use denormalized email directly (no lookup needed!)
        var userEmail = authCode.UserEmail;

        // Generate access token (JWT)
        var accessToken = jwtTokenGenerator.GenerateAccessToken(
            authCode.UserId,
            userEmail,
            client.AccessTokenLifetimeMinutes);

        // Generate refresh token
        var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(
            authCode.UserId,
            client.RefreshTokenLifetimeDays,
            null,
            cancellationToken);

        var expiresAt = now.AddMinutes(client.AccessTokenLifetimeMinutes);

        logger.LogInformation("Successfully exchanged authorization code for tokens. UserId: {UserId}",
            authCode.UserId);

        return (accessToken, refreshToken.Token, expiresAt);
    }



    /// <summary>
    /// Validates an OAuth client and redirect URI.
    /// </summary>
    public async Task<bool> ValidateClientAndRedirectUriAsync(
        string clientId,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var client = await context.OAuthClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);

        if (client == null || !client.Enabled)
        {
            logger.LogWarning("OAuth client not found or disabled: {ClientId}", clientId);
            return false;
        }

        if (!client.IsRedirectUriAllowed(redirectUri))
        {
            logger.LogWarning(
                "Redirect URI not allowed for client {ClientId}: {RedirectUri}",
                clientId,
                redirectUri);
            return false;
        }

        return true;
    }
}

