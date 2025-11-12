import { useState, useEffect } from "react";
import type { AuthState } from "../types/auth";

function App() {
  const [authState, setAuthState] = useState<AuthState>({
    isAuthenticated: false,
    isLoading: true,
  });
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<{
    type: "success" | "error";
    text: string;
  } | null>(null);

  // Check auth status on mount
  useEffect(() => {
    checkAuthStatus();
  }, []);

  const checkAuthStatus = async () => {
    try {
      const response = await chrome.runtime.sendMessage({
        type: "GET_AUTH_STATUS",
      });
      if (response.success) {
        setAuthState({ ...response.data, isLoading: false });
      } else {
        setAuthState({
          isAuthenticated: false,
          isLoading: false,
          error: response.error,
        });
      }
    } catch (error) {
      console.error("Error checking auth status:", error);
      setAuthState({ isAuthenticated: false, isLoading: false });
    }
  };

  const handleLogin = async () => {
    setAuthState({ ...authState, isLoading: true, error: undefined });
    try {
      const response = await chrome.runtime.sendMessage({
        type: "START_OAUTH_FLOW",
      });
      if (response.success) {
        setAuthState({ ...response.data, isLoading: false });
        setMessage({ type: "success", text: "Successfully logged in!" });
        setTimeout(() => setMessage(null), 3000);
      } else {
        setAuthState({
          isAuthenticated: false,
          isLoading: false,
          error: response.error,
        });
      }
    } catch (error) {
      console.error("Login error:", error);
      setAuthState({
        isAuthenticated: false,
        isLoading: false,
        error: error instanceof Error ? error.message : "Login failed",
      });
    }
  };

  const handleLogout = async () => {
    try {
      await chrome.runtime.sendMessage({ type: "LOGOUT" });
      setAuthState({ isAuthenticated: false, isLoading: false });
      setMessage({ type: "success", text: "Logged out successfully" });
      setTimeout(() => setMessage(null), 3000);
    } catch (error) {
      console.error("Logout error:", error);
    }
  };

  const handleSaveBookmark = async () => {
    setIsSaving(true);
    setMessage(null);

    try {
      // Get current tab
      const [tab] = await chrome.tabs.query({
        active: true,
        currentWindow: true,
      });

      if (!tab.url || !tab.title) {
        throw new Error("Could not get current tab information");
      }

      // Send save request to service worker
      const response = await chrome.runtime.sendMessage({
        action: "saveBookmark",
        url: tab.url,
        title: tab.title,
      });

      if (response.success) {
        setMessage({ type: "success", text: "Saved to Vowlt!" });
      } else {
        setMessage({
          type: "error",
          text: response.error || "Failed to save bookmark",
        });
      }
    } catch (error) {
      console.error("Error saving bookmark:", error);
      setMessage({
        type: "error",
        text: error instanceof Error ? error.message : "An error occurred",
      });
    } finally {
      setIsSaving(false);
      setTimeout(() => setMessage(null), 3000);
    }
  };

  if (authState.isLoading) {
    return (
      <div className="w-80 p-6 bg-white">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-2 text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  if (!authState.isAuthenticated) {
    return (
      <div className="w-80 p-6 bg-white">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-800 mb-2">Vowlt</h1>
          <p className="text-gray-600 mb-6">Bookmark Manager</p>

          {authState.error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-600">{authState.error}</p>
            </div>
          )}

          <button
            onClick={handleLogin}
            className="w-full py-3 px-4 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            Login with OAuth
          </button>

          <p className="mt-4 text-xs text-gray-500">
            Secure OAuth 2.1 authentication with PKCE
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-80 p-6 bg-white">
      <div className="mb-6">
        <h1 className="text-xl font-bold text-gray-800">Vowlt</h1>
        {authState.userEmail && (
          <p className="text-sm text-gray-600 mt-1">{authState.userEmail}</p>
        )}
      </div>

      {message && (
        <div
          className={`mb-4 p-3 rounded-lg ${
            message.type === "success"
              ? "bg-green-50 border border-green-200"
              : "bg-red-50 border border-red-200"
          }`}
        >
          <p
            className={`text-sm ${
              message.type === "success" ? "text-green-600" : "text-red-600"
            }`}
          >
            {message.text}
          </p>
        </div>
      )}

      <button
        onClick={handleSaveBookmark}
        disabled={isSaving}
        className="w-full py-3 px-4 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors mb-3"
      >
        {isSaving ? "Saving..." : "Save Current Page"}
      </button>

      <button
        onClick={handleLogout}
        className="w-full py-2 px-4 bg-gray-100 text-gray-700 font-medium rounded-lg hover:bg-gray-200 transition-colors"
      >
        Disconnect
      </button>
    </div>
  );
}

export default App;
