/**
 * PKCE (Proof Key for Code Exchange) utilities for OAuth 2.1
 * RFC 7636: https://tools.ietf.org/html/rfc7636
 */

/**
 * Generates a cryptographically secure random string for code_verifier
 * @returns Base64URL-encoded random string (43-128 characters)
 */
export function generateCodeVerifier(): string {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return base64URLEncode(array);
}

/**
 * Generates code_challenge from code_verifier using SHA-256
 * @param verifier - The code verifier string
 * @returns Base64URL-encoded SHA-256 hash
 */
export async function generateCodeChallenge(verifier: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(verifier);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return base64URLEncode(new Uint8Array(digest));
}

/**
 * Generates a random state parameter for CSRF protection
 * @returns Base64URL-encoded random string
 */
export function generateState(): string {
  const array = new Uint8Array(16);
  crypto.getRandomValues(array);
  return base64URLEncode(array);
}

/**
 * Base64URL encoding (RFC 4648 § 5)
 * @param buffer - Byte array to encode
 * @returns Base64URL-encoded string (no padding)
 */
function base64URLEncode(buffer: Uint8Array): string {
  const base64 = btoa(String.fromCharCode(...buffer));
  return base64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");
}
