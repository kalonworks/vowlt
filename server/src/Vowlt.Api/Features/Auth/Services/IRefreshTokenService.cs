using Vowlt.Api.Features.Auth.Models;

namespace Vowlt.Api.Features.Auth.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(
        Guid userId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> ValidateRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<RefreshToken> RotateRefreshTokenAsync(
        RefreshToken oldToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(
        string token,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task RemoveExpiredTokensAsync(
        CancellationToken cancellationToken = default);
}

