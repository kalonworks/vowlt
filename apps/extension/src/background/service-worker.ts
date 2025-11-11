// Background service worker for Chrome extension (Manifest V3)

const API_BASE_URL = 'http://localhost:5000/api'

// Listen for messages from popup
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === 'saveBookmark') {
    // Handle bookmark save request
    handleSaveBookmark(request.url, request.title)
      .then((result) => sendResponse(result))
      .catch((error) => sendResponse({ success: false, error: error.message }))

    // Return true to indicate we'll send response asynchronously
    return true
  }
})

/**
 * Saves a bookmark to Vowlt API
 */
async function handleSaveBookmark(url: string, title: string): Promise<{ success: boolean; error?: string }> {
  try {
    // Get auth token from storage
    const { authToken } = await chrome.storage.sync.get(['authToken'])

    if (!authToken) {
      return { success: false, error: 'Not connected. Please add your auth token.' }
    }

    // Make API request to create bookmark
    const response = await fetch(`${API_BASE_URL}/bookmarks`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authToken}`
      },
      body: JSON.stringify({
        url,
        title,
        // Note: Metadata extraction will auto-fill description, favicon, etc.
      })
    })

    if (!response.ok) {
      const errorData = await response.json().catch(() => null)
      const errorMessage = errorData?.title || errorData?.message || `HTTP ${response.status}`
      return { success: false, error: errorMessage }
    }

    const data = await response.json()

    // Show success notification
    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icons/icon48.png',
      title: 'Saved to Vowlt',
      message: `"${title}" has been saved`,
      priority: 1
    })

    return { success: true }
  } catch (error) {
    console.error('Error saving bookmark:', error)
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Network error'
    }
  }
}

// Log when service worker starts
console.log('Vowlt background service worker loaded')
