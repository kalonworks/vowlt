/**
  * PKCE (Proof Key for Code Exchange) utilities for OAuth 2.1
  * Implements RFC 7636 for secure authorization code flow
  */

/**
 * Generates a cryptographically random code verifier
 * @returns base64url-encoded random string (43-128 characters)
 */
export function generateCodeVerifier(): string {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return base64URLEncode(array);
}

/**
 * Generates a code challenge from a code verifier
 * @param verifier - The code verifier to hash
 * @returns base64url-encoded SHA-256 hash of the verifier
 */
export async function generateCodeChallenge(verifier: string): Promise<string> {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', data);
    return base64URLEncode(new Uint8Array(digest));
}

/**
 * Base64URL encoding (URL-safe base64 without padding)
 * @param buffer - Uint8Array to encode
 * @returns base64url-encoded string
 */
function base64URLEncode(buffer: Uint8Array): string {
    const base64 = btoa(String.fromCharCode(...buffer));
    return base64
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
}

/**
 * Generates a random state parameter for CSRF protection
 * @returns random state string
 */
export function generateState(): string {
    const array = new Uint8Array(16);
    crypto.getRandomValues(array);
    return base64URLEncode(array);
}
