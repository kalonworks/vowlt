namespace Vowlt.Api.Features.Auth.DTOs;

public record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

