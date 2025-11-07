using Vowlt.Api.Features.Auth.DTOs;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Auth.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, string? ipAddress = null);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
    Task<Result<bool>> RevokeAllUserTokensAsync(Guid userId, string? ipAddress = null);
}
