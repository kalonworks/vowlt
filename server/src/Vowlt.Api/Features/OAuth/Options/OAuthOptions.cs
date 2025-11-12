namespace Vowlt.Api.Features.OAuth.Options;

/// <summary>
/// Configuration options for OAuth 2.1 server.
/// </summary>
public record OAuthOptions
{
    public const string SectionName = "OAuth";

    /// <summary>
    /// Authorization code expiration in minutes (default: 10 minutes).
    /// </summary>
    public int AuthorizationCodeExpiryMinutes { get; init; } = 10;

    /// <summary>
    /// Whether to require HTTPS for redirect URIs in production (default: true).
    /// </summary>
    public bool RequireHttpsRedirectUri { get; init; } = true;

    /// <summary>
    /// Default access token lifetime in minutes (can be overridden per client).
    /// </summary>
    public int DefaultAccessTokenLifetimeMinutes { get; init; } = 5;

    /// <summary>
    /// Default refresh token lifetime in days (can be overridden per client).
    /// </summary>
    public int DefaultRefreshTokenLifetimeDays { get; init; } = 30;
}

