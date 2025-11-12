
using System.ComponentModel.DataAnnotations;

namespace Vowlt.Api.Features.OAuth.DTOs;

/// <summary>
/// Request parameters for the OAuth authorization endpoint.
/// These come as query parameters in the authorization URL.
/// </summary>
public record AuthorizeRequest
{
    /// <summary>
    /// The client identifier (e.g., "vowlt-extension")
    /// </summary>
    [Required]
    public required string ClientId { get; init; }

    /// <summary>
    /// The URI to redirect back to after authorization.
    /// Must match one of the client's registered redirect URIs.
    /// </summary>
    [Required]
    public required string RedirectUri { get; init; }

    /// <summary>
    /// PKCE code challenge derived from the code_verifier.
    /// Base64-URL encoded SHA256 hash of the code_verifier.
    /// </summary>
    [Required]
    public required string CodeChallenge { get; init; }

    /// <summary>
    /// PKCE code challenge method. Must be "S256" (SHA-256).
    /// OAuth 2.1 requires S256, plain is not allowed.
    /// </summary>
    [Required]
    public required string CodeChallengeMethod { get; init; }

    /// <summary>
    /// Optional state parameter for CSRF protection.
    /// The client should verify this matches when receiving the callback.
    /// </summary>
    public string? State { get; init; }
}

