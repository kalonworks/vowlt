using Microsoft.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Auth.Models;

namespace Vowlt.Api.Features.Auth.Services;

public class RefreshTokenService(
    VowltDbContext context,
    TimeProvider timeProvider) : IRefreshTokenService
{
    public async Task<RefreshToken> GenerateRefreshTokenAsync(
        Guid userId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateSecureToken(),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(7).UtcDateTime,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            CreatedByIp = ipAddress
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken == null)
            return null;

        if (refreshToken.RevokedAt != null)
            return null;

        if (refreshToken.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime)
            return null;

        return refreshToken;
    }

    public async Task<RefreshToken> RotateRefreshTokenAsync(
        RefreshToken oldToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        oldToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
        oldToken.RevokedByIp = ipAddress;

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = oldToken.UserId,
            Token = GenerateSecureToken(),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(7).UtcDateTime,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            CreatedByIp = ipAddress,
            ReplacedByToken = null
        };

        oldToken.ReplacedByToken = newToken.Token;

        context.RefreshTokens.Add(newToken);
        await context.SaveChangesAsync(cancellationToken);

        return newToken;
    }

    public async Task RevokeTokenAsync(
        string token,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var userToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (userToken == null) return;

        userToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
        userToken.RevokedByIp = ipAddress;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await context.RefreshTokens
            .Where(rt => rt.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime)
            .ToListAsync(cancellationToken);

        context.RefreshTokens.RemoveRange(expiredTokens);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    }
}
