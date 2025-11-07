using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.Auth.Options;

namespace Vowlt.Api.Features.Auth.Services;

public class RefreshTokenService(
    VowltDbContext context,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<RefreshToken> GenerateRefreshTokenAsync(
        Guid userId,
        string? ipAddress = null)
    {
        var token = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = userId,
            ExpiresAt = timeProvider.GetUtcNow()
                .AddDays(_jwtOptions.RefreshTokenExpiryDays)
                .UtcDateTime,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            CreatedByIp = ipAddress
        };

        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        return token;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
            return null;

        // If token was already used (revoked), it might be a security breach!
        if (refreshToken.IsRevoked)
        {
            // Revoke all tokens for this user 
            var allUserTokens = await context.RefreshTokens
                .Where(rt => rt.UserId == refreshToken.UserId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var userToken in allUserTokens)
            {
                userToken.RevokedAt = DateTime.UtcNow;
                userToken.RevokedByIp = "Auto-revoked: Token reuse detected";
            }

            await context.SaveChangesAsync();
            return null;
        }

        if (!refreshToken.IsActive)
            return null;

        return refreshToken;
    }


    public async Task<RefreshToken> RotateRefreshTokenAsync(
        RefreshToken oldToken,
        string? ipAddress = null)
    {
        var newToken = await GenerateRefreshTokenAsync(oldToken.UserId, ipAddress);

        oldToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
        oldToken.RevokedByIp = ipAddress;
        oldToken.ReplacedByToken = newToken.Token;

        await context.SaveChangesAsync();

        return newToken;
    }

    public async Task RevokeTokenAsync(string token, string? ipAddress = null)
    {
        var refreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null || !refreshToken.IsActive)
            return;

        refreshToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
        refreshToken.RevokedByIp = ipAddress;

        await context.SaveChangesAsync();
    }

    public async Task RemoveExpiredTokensAsync()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiredTokens = await context.RefreshTokens
            .Where(rt => rt.ExpiresAt < now)
            .ToListAsync();

        context.RefreshTokens.RemoveRange(expiredTokens);
        await context.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
