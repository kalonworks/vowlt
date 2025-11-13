using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Auth.DTOs;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Auth.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    VowltDbContext context,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenService refreshTokenService,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<UserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<UserDto>.Failure("Email already registered");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserDto>.Failure($"Registration failed: {errors}");
        }

        // Return user DTO (no tokens - OAuth will handle that)
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName ?? user.Email!,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Result<UserDto>.Success(userDto);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var token = await refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
        if (token == null)
        {
            return Result<AuthResponse>.Failure("Invalid or expired refresh token");
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user == null)
        {
            return Result<AuthResponse>.Failure("User not found");
        }

        if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            await refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, cancellationToken);
            return Result<AuthResponse>.Failure("Account is locked");
        }

        var newRefreshToken = await refreshTokenService.RotateRefreshTokenAsync(
            token,
            ipAddress,
            cancellationToken);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.DisplayName ?? user.Email!);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName ?? user.Email!,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<bool>> RevokeAllUserTokensAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (!activeTokens.Any())
        {
            return Result<bool>.Success(true);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = ipAddress;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
