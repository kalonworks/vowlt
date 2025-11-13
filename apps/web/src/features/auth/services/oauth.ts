import {
  generateCodeVerifier,
  generateCodeChallenge,
  generateState,
} from "../utils/pkce";
import type { AuthResponse } from "../types";

interface TokenResponseDto {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token: string;
}

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5000";
const CLIENT_ID = "vowlt-spa";
const REDIRECT_URI = `${window.location.origin}/auth/callback`;

/**
 * OAuth 2.1 service for SPA authentication
 */

/**
 * Starts OAuth flow by redirecting to authorization endpoint
 * Generates PKCE parameters and stores them in sessionStorage
 */
export async function startOAuthFlow(): Promise<void> {
  // Generate PKCE parameters
  const codeVerifier = generateCodeVerifier();
  const codeChallenge = await generateCodeChallenge(codeVerifier);
  const state = generateState();

  // Store in sessionStorage (will be read in callback)
  sessionStorage.setItem("oauth_code_verifier", codeVerifier);
  sessionStorage.setItem("oauth_state", state);

  // Build authorization URL
  const authUrl = new URL(`${API_BASE_URL}/oauth/authorize`);
  authUrl.searchParams.set("client_id", CLIENT_ID);
  authUrl.searchParams.set("redirect_uri", REDIRECT_URI);
  authUrl.searchParams.set("response_type", "code");
  authUrl.searchParams.set("code_challenge", codeChallenge);
  authUrl.searchParams.set("code_challenge_method", "S256");
  authUrl.searchParams.set("state", state);

  // Redirect to authorization server
  window.location.href = authUrl.toString();
}

/**
 * Handles OAuth callback - extracts code and exchanges for tokens
 * Called from /auth/callback route
 * @param callbackUrl - The full callback URL with query parameters
 * @returns Auth response with tokens
 */
export async function handleOAuthCallback(
  callbackUrl: string
): Promise<AuthResponse> {
  const url = new URL(callbackUrl);
  const code = url.searchParams.get("code");
  const state = url.searchParams.get("state");

  if (!code) {
    throw new Error("No authorization code in callback");
  }

  // Verify state (CSRF protection)
  const storedState = sessionStorage.getItem("oauth_state");
  if (state !== storedState) {
    throw new Error("State parameter mismatch - possible CSRF attack");
  }

  // Get code verifier
  const codeVerifier = sessionStorage.getItem("oauth_code_verifier");
  if (!codeVerifier) {
    throw new Error("Code verifier not found");
  }

  // Clean up session storage
  sessionStorage.removeItem("oauth_code_verifier");
  sessionStorage.removeItem("oauth_state");

  // Exchange code for tokens
  const response = await fetch(`${API_BASE_URL}/oauth/token`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      grant_type: "authorization_code",
      code,
      client_id: CLIENT_ID,
      redirect_uri: REDIRECT_URI,
      code_verifier: codeVerifier,
    }),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Token exchange failed: ${error}`);
  }

  const data = (await response.json()) as TokenResponseDto;

  // Transform to AuthResponse format (user will be populated from JWT in callback)
  return {
    accessToken: data.access_token,
    refreshToken: data.refresh_token,
    expiresAt: new Date(Date.now() + data.expires_in * 1000).toISOString(),
    user: {
      id: "", // Will be populated from JWT in callback
      email: "", // Will be populated from JWT in callback
      displayName: "", // Will be populated from JWT in callback
      createdAt: "", // Will be populated from JWT in callback
    },
  };
}
