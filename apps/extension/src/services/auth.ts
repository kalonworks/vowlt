import type { TokenResponse, AuthState } from '../types/auth';
import { generateCodeVerifier, generateCodeChallenge, generateState } from './pkce';
import { getTokens, setTokens, clearTokens, isTokenExpired } from './storage';
import { OAUTH_CONFIG } from '../config/oauth';

/**
 * OAuth 2.1 authentication service with PKCE
 * Handles authorization flow, token exchange, and token refresh
 */

/**
 * Initiates OAuth authorization flow using chrome.identity.launchWebAuthFlow
 * @returns Authorization code from successful redirect
 * @throws Error if user cancels or flow fails
 */
export async function startOAuthFlow(): Promise<string> {
    // Generate PKCE parameters
    const codeVerifier = generateCodeVerifier();
    const codeChallenge = await generateCodeChallenge(codeVerifier);
    const state = generateState();

    // Store code verifier for token exchange
    await chrome.storage.local.set({
        oauth_code_verifier: codeVerifier,
        oauth_state: state
    });

    // Build authorization URL
    const authUrl = new URL(OAUTH_CONFIG.apiUrl + OAUTH_CONFIG.authorizePath);
    authUrl.searchParams.set('client_id', OAUTH_CONFIG.clientId);
    authUrl.searchParams.set('redirect_uri', OAUTH_CONFIG.redirectUri);
    authUrl.searchParams.set('response_type', 'code');
    authUrl.searchParams.set('code_challenge', codeChallenge);
    authUrl.searchParams.set('code_challenge_method', 'S256');
    authUrl.searchParams.set('state', state);

    console.log('🔐 OAuth URL:', authUrl.toString());
    console.log('📍 Redirect URI:', OAUTH_CONFIG.redirectUri);
    console.log('🆔 Client ID:', OAUTH_CONFIG.clientId);

    // Launch web auth flow
    return new Promise((resolve, reject) => {
        chrome.identity.launchWebAuthFlow(
            {
                url: authUrl.toString(),
                interactive: true,
            },
            (redirectUrl) => {
                if (chrome.runtime.lastError) {
                    console.error('❌ OAuth error:', chrome.runtime.lastError);
                    reject(new Error(chrome.runtime.lastError.message));
                    return;
                }

                if (!redirectUrl) {
                    reject(new Error('No redirect URL received'));
                    return;
                }

                console.log('✅ OAuth redirect:', redirectUrl);

                // Extract authorization code from redirect URL
                const url = new URL(redirectUrl);
                const code = url.searchParams.get('code');
                const returnedState = url.searchParams.get('state');

                if (!code) {
                    reject(new Error('No authorization code in redirect'));
                    return;
                }

                // Verify state parameter (CSRF protection)
                chrome.storage.local.get(['oauth_state'], (result) => {
                    if (returnedState !== result.oauth_state) {
                        reject(new Error('State parameter mismatch (CSRF protection)'));
                        return;
                    }

                    // Clean up state
                    chrome.storage.local.remove(['oauth_state']);
                    resolve(code);
                });
            }
        );
    });
}



/**
 * Exchanges authorization code for access and refresh tokens
 * @param code - Authorization code from OAuth flow
 * @returns Token response with access_token, refresh_token, expires_in
 * @throws Error if token exchange fails
 */
export async function exchangeCodeForTokens(code: string): Promise<TokenResponse> {
    // Retrieve stored code verifier
    const result = await chrome.storage.local.get(['oauth_code_verifier']);
    const codeVerifier = result.oauth_code_verifier;

    if (!codeVerifier) {
        throw new Error('Code verifier not found');
    }

    // Clean up code verifier
    await chrome.storage.local.remove(['oauth_code_verifier']);

    // Exchange code for tokens
    const response = await fetch(OAUTH_CONFIG.apiUrl + OAUTH_CONFIG.tokenPath, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            grant_type: 'authorization_code',
            code,
            client_id: OAUTH_CONFIG.clientId,
            redirect_uri: OAUTH_CONFIG.redirectUri,
            code_verifier: codeVerifier,
        }),
    });

    if (!response.ok) {
        const error = await response.text();
        throw new Error(`Token exchange failed: ${error}`);
    }

    return response.json();
}

/**
 * Refreshes an expired access token using refresh token
 * @returns New token response
 * @throws Error if refresh fails
 */
export async function refreshAccessToken(): Promise<TokenResponse> {
    const tokens = await getTokens();

    if (!tokens?.refreshToken) {
        throw new Error('No refresh token available');
    }

    const response = await fetch(OAUTH_CONFIG.apiUrl + OAUTH_CONFIG.tokenPath, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            grant_type: 'refresh_token',
            refresh_token: tokens.refreshToken,
            client_id: OAUTH_CONFIG.clientId,
        }),
    });

    if (!response.ok) {
        // Refresh token invalid/expired - clear tokens
        await clearTokens();
        throw new Error('Refresh token expired - please login again');
    }

    return response.json();
}

/**
 * Gets a valid access token, automatically refreshing if expired
 * @returns Valid access token
 * @throws Error if not authenticated or refresh fails
 */
export async function getValidAccessToken(): Promise<string> {
    const tokens = await getTokens();

    if (!tokens) {
        throw new Error('Not authenticated');
    }

    // Token still valid - return it
    if (!isTokenExpired(tokens)) {
        return tokens.accessToken;
    }

    // Token expired - refresh it
    const newTokenResponse = await refreshAccessToken();

    // Store new tokens
    await setTokens({
        accessToken: newTokenResponse.access_token,
        refreshToken: newTokenResponse.refresh_token,
        expiresAt: Date.now() + (newTokenResponse.expires_in * 1000),
        userEmail: tokens.userEmail, // Preserve email
    });

    return newTokenResponse.access_token;
}

/**
 * Performs complete OAuth login flow
 * @returns Authentication state after successful login
 * @throws Error if login fails
 */
export async function login(): Promise<AuthState> {
    // Step 1: Start OAuth flow and get authorization code
    const code = await startOAuthFlow();

    // Step 2: Exchange code for tokens
    const tokenResponse = await exchangeCodeForTokens(code);

    // Step 3: Decode JWT to get user email
    const payload = JSON.parse(atob(tokenResponse.access_token.split('.')[1]));
    const userEmail = payload.email;

    // Step 4: Store tokens
    await setTokens({
        accessToken: tokenResponse.access_token,
        refreshToken: tokenResponse.refresh_token,
        expiresAt: Date.now() + (tokenResponse.expires_in * 1000),
        userEmail,
    });

    return {
        isAuthenticated: true,
        userEmail,
    };
}

/**
 * Logs out user and clears all stored tokens
 */
export async function logout(): Promise<void> {
    await clearTokens();
}

/**
 * Gets current authentication state
 * @returns Current auth state
 */
export async function getAuthState(): Promise<AuthState> {
    const tokens = await getTokens();

    if (!tokens || isTokenExpired(tokens)) {
        return { isAuthenticated: false };
    }

    return {
        isAuthenticated: true,
        userEmail: tokens.userEmail,
    };
}

