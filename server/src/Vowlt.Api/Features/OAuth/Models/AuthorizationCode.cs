namespace Vowlt.Api.Features.OAuth.Models;

public class AuthorizationCode
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string CodeChallenge { get; private set; } = string.Empty;
    public string CodeChallengeMethod { get; private set; } = "S256";
    public Guid UserId { get; private set; }
    public string UserEmail { get; private set; } = string.Empty; // Denormalized for performance
    public string UserDisplayName { get; private set; } = string.Empty; // Denormalized displayName
    public string ClientId { get; private set; } = string.Empty;
    public string RedirectUri { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool Used { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private AuthorizationCode() { }

    public static AuthorizationCode Create(
        Guid userId,
        string userEmail,
        string userDisplayName, // NEW: Accept displayName parameter
        string clientId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod,
        string? state,
        DateTime now)
    {
        // Generate cryptographically secure random code (base64url encoding)
        var codeBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var code = Convert.ToBase64String(codeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new AuthorizationCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            UserId = userId,
            UserEmail = userEmail,
            UserDisplayName = userDisplayName, // NEW: Store displayName
            ClientId = clientId,
            RedirectUri = redirectUri,
            State = state,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(10), // OAuth 2.1 standard: 10 min expiry
            Used = false
        };
    }

    public void MarkAsUsed(DateTime now)
    {
        Used = true;
        UsedAt = now;
    }

    public bool IsValid(DateTime now)
    {
        return !Used && now < ExpiresAt;
    }
}

