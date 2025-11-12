
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Vowlt.Api.Features.OAuth.DTOs;

/// <summary>
/// Request parameters for the OAuth authorization endpoint.
/// These come as query parameters in the authorization URL.
/// </summary>
public record AuthorizeRequest
{
    [Required]
    [FromQuery(Name = "client_id")]
    public required string ClientId { get; init; }

    [Required]
    [FromQuery(Name = "redirect_uri")]
    public required string RedirectUri { get; init; }

    [Required]
    [FromQuery(Name = "code_challenge")]
    public required string CodeChallenge { get; init; }

    [Required]
    [FromQuery(Name = "code_challenge_method")]
    public required string CodeChallengeMethod { get; init; }

    [FromQuery(Name = "state")]
    public string? State { get; init; }
}


