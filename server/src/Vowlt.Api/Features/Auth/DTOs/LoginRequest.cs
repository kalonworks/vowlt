namespace Vowlt.Api.Features.Auth.DTOs;

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

