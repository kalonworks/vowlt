import type { OAuthConfig } from '../types/auth';

/**
 * OAuth configuration for Vowlt API
 */

const isDevelopment = import.meta.env.MODE === 'development';

export const OAUTH_CONFIG: OAuthConfig = {
    apiUrl: isDevelopment ? 'http://localhost:5000' : 'https://api.vowlt.com',
    clientId: 'vowlt-chrome-extension', // Always use extension client (has *.chromiumapp.org redirect)
    redirectUri: chrome.identity ? chrome.identity.getRedirectURL() : 'https://vowlt.com/oauth/callback',
    authorizePath: '/oauth/authorize',
    tokenPath: '/oauth/token',
};
