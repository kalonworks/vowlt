namespace Vowlt.Api.Features.Auth.Services;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email);
}