namespace Vowlt.Api.Extensions.Logging;

public static partial class StartupLogging
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Environment file loaded from {envPath}")]
    public static partial void EnvironmentFileLoaded(
        this ILogger logger,
        string envPath);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Environment file not found at {envPath}, using system environment variables")]
    public static partial void EnvironmentFileNotFound(
        this ILogger logger,
        string envPath);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Database connection configured: Host={host}, Database={database}, User={username}")]
    public static partial void DatabaseConfigured(
        this ILogger logger,
        string host,
        string database,
        string username);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Rate limits configured from {source}: Login={loginLimit}/{loginWindow}, Register={registerLimit}/{registerWindow}, Refresh={refreshLimit}/{refreshWindow}")]
    public static partial void RateLimitsConfigured(
        this ILogger logger,
        string source,
        int loginLimit,
        string loginWindow,
        int registerLimit,
        string registerWindow,
        int refreshLimit,
        string refreshWindow);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "JWT authentication configured: Issuer={issuer}, Audience={audience}, AccessTokenExpiry={accessExpiry}min")]
    public static partial void JwtConfigured(
        this ILogger logger,
        string issuer,
        string audience,
        int accessExpiry);
}
