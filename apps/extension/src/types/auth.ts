/**
  * Authentication and OAuth type definitions
  */

/**
 * OAuth token response from /oauth/token endpoint
 */
export interface TokenResponse {
    access_token: string;
    token_type: 'Bearer';
    expires_in: number; // seconds until expiration
    refresh_token: string;
}

/**
 * Stored token data in chrome.storage.local
 */
export interface StoredTokens {
    accessToken: string;
    refreshToken: string;
    expiresAt: number; // Unix timestamp (ms)
    userEmail?: string;
}

/**
 * Current authentication state
 */
export interface AuthState {
    isAuthenticated: boolean;
    userEmail?: string;
    isLoading?: boolean;
    error?: string;
}

/**
 * OAuth configuration
 */
export interface OAuthConfig {
    apiUrl: string;
    clientId: string;
    redirectUri: string;
    authorizePath: string;
    tokenPath: string;
}

/**
 * Messages sent between popup and service worker
 */
export type AuthMessage =
    | { type: 'START_OAUTH_FLOW' }
    | { type: 'GET_AUTH_STATUS' }
    | { type: 'LOGOUT' }
    | { type: 'GET_VALID_TOKEN' };

/**
 * Responses from service worker to popup
 */
export type AuthMessageResponse =
    | { success: true; data: AuthState }
    | { success: true; data: { token: string } }
    | { success: false; error: string };
