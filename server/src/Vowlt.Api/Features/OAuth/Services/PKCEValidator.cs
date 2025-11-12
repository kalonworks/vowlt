using System.Security.Cryptography;
using System.Text;

namespace Vowlt.Api.Features.OAuth.Services;

/// <summary>
/// Validates PKCE (Proof Key for Code Exchange) challenges.
/// Implements SHA-256 hashing as required by OAuth 2.1.
/// </summary>
public class PKCEValidator
{
    /// <summary>
    /// Validates that code_verifier matches the stored code_challenge.
    /// </summary>
    /// <param name="codeVerifier">The code verifier sent by the client</param>
    /// <param name="codeChallenge">The code challenge stored during authorization</param>
    /// <param name="codeChallengeMethod">The method used (must be "S256")</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateCodeVerifier(
        string codeVerifier,
        string codeChallenge,
        string codeChallengeMethod)
    {
        // Only S256 (SHA-256) is supported (OAuth 2.1 requirement)
        if (codeChallengeMethod != "S256")
        {
            return false;
        }

        // Validate code_verifier format (43-128 characters, base64url)
        if (string.IsNullOrWhiteSpace(codeVerifier) ||
            codeVerifier.Length < 43 ||
            codeVerifier.Length > 128)
        {
            return false;
        }

        // Generate code_challenge from code_verifier
        var computedChallenge = GenerateCodeChallenge(codeVerifier);

        // Compare with stored code_challenge (constant-time comparison)
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedChallenge),
            Encoding.UTF8.GetBytes(codeChallenge));
    }

    /// <summary>
    /// Generates a code_challenge from a code_verifier using SHA-256.
    /// </summary>
    private static string GenerateCodeChallenge(string codeVerifier)
    {
        // Hash the code_verifier with SHA-256
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);

        // Convert to base64url (no padding, URL-safe)
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
