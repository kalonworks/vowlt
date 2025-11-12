import type { StoredTokens } from '../types/auth';

/**
 * Secure token storage using chrome.storage.local
 * Tokens are stored locally (not synced across devices) for security
 */

const TOKENS_KEY = 'vowlt_tokens';

/**
 * Retrieves stored tokens from chrome.storage.local
 * @returns Stored tokens or null if not found
 */
export async function getTokens(): Promise<StoredTokens | null> {
    return new Promise((resolve) => {
        chrome.storage.local.get([TOKENS_KEY], (result) => {
            resolve(result[TOKENS_KEY] || null);
        });
    });
}

/**
 * Stores tokens in chrome.storage.local
 * @param tokens - Token data to store
 */
export async function setTokens(tokens: StoredTokens): Promise<void> {
    return new Promise((resolve) => {
        chrome.storage.local.set({ [TOKENS_KEY]: tokens }, () => {
            resolve();
        });
    });
}

/**
 * Removes all stored tokens (logout)
 */
export async function clearTokens(): Promise<void> {
    return new Promise((resolve) => {
        chrome.storage.local.remove([TOKENS_KEY], () => {
            resolve();
        });
    });
}

/**
 * Checks if stored access token is expired
 * @param tokens - Stored token data
 * @param bufferSeconds - Extra time buffer to refresh before actual expiry (default 60s)
 * @returns true if token is expired or will expire soon
 */
export function isTokenExpired(tokens: StoredTokens, bufferSeconds: number = 60): boolean {
    const now = Date.now();
    const expiryWithBuffer = tokens.expiresAt - (bufferSeconds * 1000);
    return now >= expiryWithBuffer;
}

/**
 * Checks if user is authenticated (has valid tokens)
 * @returns true if tokens exist and are not expired
 */
export async function isAuthenticated(): Promise<boolean> {
    const tokens = await getTokens();
    if (!tokens) return false;
    return !isTokenExpired(tokens);
}
