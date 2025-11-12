namespace Vowlt.Api.Features.OAuth.Models;

/// <summary>
/// Represents an OAuth client application (extension, mobile app, etc.).
/// Defines allowed redirect URIs and client-specific settings.
/// </summary>
public class OAuthClient
{
    public string ClientId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    // Allowed redirect URIs (comma-separated for simplicity)
    // Example: "https://abc123.chromiumapp.org/*,https://vowlt.com/callback"
    public string AllowedRedirectUris { get; private set; } = string.Empty;

    // Token lifetimes (in minutes)
    public int AccessTokenLifetimeMinutes { get; private set; } = 5;
    public int RefreshTokenLifetimeDays { get; private set; } = 30;

    // Metadata
    public DateTime CreatedAt { get; private set; }
    public bool Enabled { get; private set; } = true;

    // EF Core requires parameterless constructor
    private OAuthClient() { }

    /// <summary>
    /// Creates a new OAuth client.
    /// </summary>
    public static OAuthClient Create(
        string clientId,
        string name,
        string description,
        string allowedRedirectUris,
        int accessTokenLifetimeMinutes,
        int refreshTokenLifetimeDays,
        DateTime now)
    {
        return new OAuthClient
        {
            ClientId = clientId,
            Name = name,
            Description = description,
            AllowedRedirectUris = allowedRedirectUris,
            AccessTokenLifetimeMinutes = accessTokenLifetimeMinutes,
            RefreshTokenLifetimeDays = refreshTokenLifetimeDays,
            CreatedAt = now,
            Enabled = true
        };
    }

    /// <summary>
    /// Checks if a redirect URI is allowed for this client.
    /// </summary>
    public bool IsRedirectUriAllowed(string redirectUri)
    {
        var allowedUris = AllowedRedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var allowedUri in allowedUris)
        {
            var pattern = allowedUri.Trim();

            // Support wildcard matching (e.g., "https://*.chromiumapp.org/*")
            if (pattern.Contains('*'))
            {
                var regex = new System.Text.RegularExpressions.Regex(
                    "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                        .Replace("\\*", ".*") + "$");

                if (regex.IsMatch(redirectUri))
                    return true;
            }
            else if (pattern.Equals(redirectUri, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
