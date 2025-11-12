using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Vowlt.Api.Features.OAuth.DTOs;

/// <summary>
/// Request body for the OAuth token endpoint.
/// Used to exchange an authorization code for access and refresh tokens.
/// </summary>
public record TokenRequest
{
    /// <summary>
    /// The grant type. Must be "authorization_code" for this flow.
    /// </summary>
    [Required]
    [JsonPropertyName("grant_type")]
    public required string GrantType { get; init; }

    /// <summary>
    /// The authorization code received from the /authorize endpoint.
    /// </summary>
    [Required]
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// PKCE code verifier - the original random string.
    /// Server will hash this and compare to the code_challenge.
    /// </summary>
    [Required]
    [JsonPropertyName("code_verifier")]
    public required string CodeVerifier { get; init; }

    /// <summary>
    /// The client identifier. Must match the authorize request.
    /// </summary>
    [Required]
    [JsonPropertyName("client_id")]
    public required string ClientId { get; init; }

    /// <summary>
    /// The redirect URI. Must exactly match the authorize request.
    /// </summary>
    [Required]
    [JsonPropertyName("redirect_uri")]
    public required string RedirectUri { get; init; }
}
