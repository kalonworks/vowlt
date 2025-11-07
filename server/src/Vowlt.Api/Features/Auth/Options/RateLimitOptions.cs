namespace Vowlt.Api.Features.Auth.Options;

public class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    public LoginLimitOptions Login { get; set; } = new();
    public RegisterLimitOptions Register { get; set; } = new();
    public RefreshLimitOptions Refresh { get; set; } = new();
}

public class LoginLimitOptions
{
    public int PermitLimit { get; set; } = 5;
    public int WindowMinutes { get; set; } = 1;
}

public class RegisterLimitOptions
{
    public int PermitLimit { get; set; } = 3;
    public int WindowHours { get; set; } = 1;
}

public class RefreshLimitOptions
{
    public int PermitLimit { get; set; } = 30;
    public int WindowHours { get; set; } = 1;
}
