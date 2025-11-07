namespace Vowlt.Api.Extensions.Logging;

public static partial class AuthenticationLogging
{
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "User registered: UserId={userId}, Email={email}, IpAddress={ipAddress}")]
    public static partial void UserRegistered(
        this ILogger logger,
        Guid userId,
        string email,
        string? ipAddress);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "User login successful: UserId={userId}, Email={email}, IpAddress={ipAddress}")]
    public static partial void LoginSuccessful(
        this ILogger logger,
        Guid userId,
        string email,
        string? ipAddress);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Login failed: Email={email}, Reason={reason}, IpAddress={ipAddress}")]
    public static partial void LoginFailed(
        this ILogger logger,
        string email,
        string reason,
        string? ipAddress);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "Token refreshed: UserId={userId}, IpAddress={ipAddress}")]
    public static partial void TokenRefreshed(
        this ILogger logger,
        Guid userId,
        string? ipAddress);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "Token refresh failed: Reason={reason}, IpAddress={ipAddress}")]
    public static partial void TokenRefreshFailed(
        this ILogger logger,
        string reason,
        string? ipAddress);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "User logged out: UserId={userId}, TokensRevoked={tokenCount}")]
    public static partial void UserLoggedOut(
        this ILogger logger,
        Guid userId,
        int tokenCount);
}
