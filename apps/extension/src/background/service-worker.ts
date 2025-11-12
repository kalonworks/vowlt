import type { AuthMessage, AuthMessageResponse } from '../types/auth';
import * as auth from '../services/auth';

/**
 * Background service worker (Manifest V3)
 * Handles OAuth flow, token management, and API requests
 */

// Single message listener for all messages
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  handleMessage(message)
    .then(sendResponse)
    .catch(error => {
      console.error('Message handler error:', error);
      sendResponse({ success: false, error: error.message });
    });
  return true; // Keep channel open for async response
});

/**
 * Handle all messages from popup
 */
async function handleMessage(message: any): Promise<any> {
  // Handle bookmark save
  if (message.action === 'saveBookmark') {
    return handleSaveBookmark(message.url, message.title);
  }

  // Handle auth messages
  const authMessage = message as AuthMessage;

  try {
    switch (authMessage.type) {
      case 'START_OAUTH_FLOW': {
        const authState = await auth.login();
        return { success: true, data: authState };
      }

      case 'GET_AUTH_STATUS': {
        const authState = await auth.getAuthState();
        return { success: true, data: authState };
      }

      case 'LOGOUT': {
        await auth.logout();
        return { success: true, data: { isAuthenticated: false } };
      }

      case 'GET_VALID_TOKEN': {
        const token = await auth.getValidAccessToken();
        return { success: true, data: { token } };
      }

      default:
        return { success: false, error: 'Unknown message type' };
    }
  } catch (error) {
    console.error('Error handling message:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Saves a bookmark to Vowlt API with OAuth authentication
 */
async function handleSaveBookmark(url: string, title: string): Promise<{ success: boolean; error?: string }> {
  try {
    // Get valid access token (auto-refreshes if needed)
    const accessToken = await auth.getValidAccessToken();

    // Make API request
    const response = await fetch('http://localhost:5000/api/bookmarks', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ url, title }),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`API error: ${response.status} - ${errorText}`);
    }

    // Show success notification
    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icon-128.png',
      title: 'Saved to Vowlt!',
      message: `"${title}" has been saved to your bookmarks.`,
    });

    return { success: true };
  } catch (error) {
    console.error('Error saving bookmark:', error);

    const errorMessage = error instanceof Error ? error.message : 'Unknown error';

    // Show error notification
    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icon-128.png',
      title: 'Failed to save',
      message: errorMessage,
    });

    return { success: false, error: errorMessage };
  }
}
