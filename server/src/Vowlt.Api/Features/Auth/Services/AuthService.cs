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
    RefreshTokenService refreshTokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure("Email already registered");
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
            return Result<AuthResponse>.Failure($"Registration failed: {errors}");
        }

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress = null)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid credentials");
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Result<AuthResponse>.Failure("Invalid credentials");
        }

        user.LastLoginAt = timeProvider.GetUtcNow().UtcDateTime;
        await userManager.UpdateAsync(user);

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null)
    {
        var token = await refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
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
            await refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress);
            return Result<AuthResponse>.Failure("Account is locked");
        }

        // Optional: Check if email is confirmed (I will add email verification later)
        // if (!user.EmailConfirmed)
        // {
        //     return Result<AuthResponse>.Failure("Email not confirmed");
        // }


        var newRefreshToken = await refreshTokenService.RotateRefreshTokenAsync(
            token,
            ipAddress);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!);

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
      string? ipAddress = null)
    {
        var activeTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

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

        await context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }



    private async Task<Result<AuthResponse>> GenerateAuthResponseAsync(
        ApplicationUser user,
        string? ipAddress = null)
    {
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!);
        var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(
            user.Id,
            ipAddress);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
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
}
