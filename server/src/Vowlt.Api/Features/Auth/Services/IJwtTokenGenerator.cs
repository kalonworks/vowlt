namespace Vowlt.Api.Features.Auth.Services;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email, string displayName);
    string GenerateAccessToken(Guid userId, string email, string displayName, int lifetimeMinutes);
}

