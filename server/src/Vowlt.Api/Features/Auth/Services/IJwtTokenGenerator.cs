namespace Vowlt.Api.Features.Auth.Services;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateAccessToken(Guid userId, string email, int lifetimeMinutes);
}

