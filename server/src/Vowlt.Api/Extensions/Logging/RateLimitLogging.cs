namespace Vowlt.Api.Extensions.Logging;

public static partial class RateLimitLogging
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded: Policy={policy}, PartitionKey={partitionKey}, Endpoint={endpoint}")]
    public static partial void RateLimitExceeded(
        this ILogger logger,
        string policy,
        string partitionKey,
        string endpoint);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Rate limit policy registered: Name={policyName}, Limit={limit}, Window={window}")]
    public static partial void RateLimitPolicyRegistered(
        this ILogger logger,
        string policyName,
        int limit,
        string window);
}
