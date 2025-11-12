using System.Text.Json.Serialization;

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
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    /// <summary>
    /// The token type. Always "Bearer" for JWT tokens.
    /// </summary>
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    /// <summary>
    /// Number of seconds until the access token expires.
    /// Typically 300 (5 minutes).
    /// </summary>
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }

    /// <summary>
    /// The refresh token used to obtain new access tokens.
    /// Long-lived (typically 30 days). Store securely.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }
}
