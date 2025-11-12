namespace Vowlt.Api.Features.OAuth.DTOs;

/// <summary>
/// Response returned from the OAuth token endpoint.
/// Contains the access token, refresh token, and expiration info.
/// </summary>
public record TokenResponse
{
    /// <summary>
    /// The JWT access token used to authenticate API requests.
    /// Include in Authorization header as "Bearer {access_token}".
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The token type. Always "Bearer" for JWT tokens.
    /// </summary>
    public required string TokenType { get; init; }

    /// <summary>
    /// Number of seconds until the access token expires.
    /// Typically 300 (5 minutes).
    /// </summary>
    public required int ExpiresIn { get; init; }

    /// <summary>
    /// The refresh token used to obtain new access tokens.
    /// Long-lived (typically 30 days). Store securely.
    /// </summary>
    public required string RefreshToken { get; init; }
}

