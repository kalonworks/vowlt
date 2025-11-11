import { useState, useEffect } from "react";

export default function App() {
  const [isConnected, setIsConnected] = useState(false);
  const [token, setToken] = useState("");
  const [status, setStatus] = useState<"idle" | "saving" | "success" | "error">(
    "idle"
  );
  const [message, setMessage] = useState("");

  // Check if user is connected on mount
  useEffect(() => {
    chrome.storage.sync.get(["authToken"], (result) => {
      if (result.authToken) {
        setIsConnected(true);
        setToken(result.authToken);
      }
    });
  }, []);

  // Save token to chrome.storage
  const handleConnect = () => {
    if (!token.trim()) {
      setMessage("Please enter a token");
      return;
    }

    chrome.storage.sync.set({ authToken: token }, () => {
      setIsConnected(true);
      setMessage("Connected successfully!");
      setTimeout(() => setMessage(""), 2000);
    });
  };

  // Disconnect (remove token)
  const handleDisconnect = () => {
    chrome.storage.sync.remove(["authToken"], () => {
      setIsConnected(false);
      setToken("");
      setMessage("Disconnected");
      setTimeout(() => setMessage(""), 2000);
    });
  };

  // Save current page as bookmark
  const handleSaveBookmark = async () => {
    setStatus("saving");
    setMessage("Saving...");

    try {
      // Get current tab info
      const [tab] = await chrome.tabs.query({
        active: true,
        currentWindow: true,
      });

      if (!tab.url || !tab.title) {
        throw new Error("Could not get page information");
      }

      // Send message to background script to save bookmark
      chrome.runtime.sendMessage(
        {
          action: "saveBookmark",
          url: tab.url,
          title: tab.title,
        },
        (response) => {
          if (response.success) {
            setStatus("success");
            setMessage("✓ Saved to Vowlt!");
            setTimeout(() => {
              setStatus("idle");
              setMessage("");
            }, 2000);
          } else {
            setStatus("error");
            setMessage(`Error: ${response.error || "Unknown error"}`);
            setTimeout(() => {
              setStatus("idle");
              setMessage("");
            }, 3000);
          }
        }
      );
    } catch (error) {
      setStatus("error");
      setMessage(
        `Error: ${error instanceof Error ? error.message : "Unknown error"}`
      );
      setTimeout(() => {
        setStatus("idle");
        setMessage("");
      }, 3000);
    }
  };

  return (
    <div className="w-80 p-4 bg-white">
      <div className="mb-4">
        <h1 className="text-xl font-bold text-gray-900">Vowlt</h1>
        <p className="text-sm text-gray-600">Bookmark Manager</p>
      </div>

      {!isConnected ? (
        // Connect screen
        <div className="space-y-3">
          <div>
            <label
              htmlFor="token"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Auth Token
            </label>
            <input
              id="token"
              type="password"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              placeholder="Paste your token here"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              onKeyDown={(e) => e.key === "Enter" && handleConnect()}
            />
            <p className="mt-1 text-xs text-gray-500">
              Get your token from Vowlt settings
            </p>
          </div>

          <button
            onClick={handleConnect}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition-colors"
          >
            Connect
          </button>

          {message && (
            <p className="text-sm text-center text-gray-600">{message}</p>
          )}
        </div>
      ) : (
        // Save bookmark screen
        <div className="space-y-3">
          <button
            onClick={handleSaveBookmark}
            disabled={status === "saving"}
            className={`w-full py-2 px-4 rounded-md transition-colors ${
              status === "success"
                ? "bg-green-600 text-white"
                : status === "error"
                ? "bg-red-600 text-white"
                : "bg-blue-600 text-white hover:bg-blue-700"
            } disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {status === "saving"
              ? "Saving..."
              : status === "success"
              ? "Saved!"
              : "Save Current Page"}
          </button>

          {message && (
            <p
              className={`text-sm text-center ${
                status === "error" ? "text-red-600" : "text-gray-600"
              }`}
            >
              {message}
            </p>
          )}

          <button
            onClick={handleDisconnect}
            className="w-full text-sm text-gray-600 hover:text-gray-800 py-1"
          >
            Disconnect
          </button>
        </div>
      )}
    </div>
  );
}
