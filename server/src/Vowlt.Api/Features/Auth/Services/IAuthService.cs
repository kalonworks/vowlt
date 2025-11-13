using Vowlt.Api.Features.Auth.DTOs;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Auth.Services;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RevokeAllUserTokensAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
