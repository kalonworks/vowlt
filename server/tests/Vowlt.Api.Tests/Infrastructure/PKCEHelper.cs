using System.Security.Cryptography;
using System.Text;

namespace Vowlt.Api.Tests.Infrastructure;

/// <summary>
/// PKCE (Proof Key for Code Exchange) helper - implements RFC 7636.
/// Generates code_verifier and code_challenge for OAuth testing.
/// </summary>
public static class PKCEHelper
{
    /// <summary>
    /// Generates a cryptographically random code_verifier.
    /// RFC 7636: 43-128 characters, [A-Z][a-z][0-9]-._~
    /// </summary>
    public static string GenerateCodeVerifier()
    {
        var bytes = new byte[32]; // 32 bytes = 43 chars base64url
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Generates code_challenge from code_verifier using S256 method.
    /// S256 = BASE64URL(SHA256(ASCII(code_verifier)))
    /// </summary>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Base64URL encoding per RFC 4648 Section 5.
    /// URL-safe: uses - and _ instead of + and /, no padding =
    /// </summary>
    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')           // Remove padding
            .Replace('+', '-')      // URL-safe
            .Replace('/', '_');     // URL-safe
    }
}

