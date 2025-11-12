import { useEffect, useState, useRef } from "react";
import { useNavigate } from "@tanstack/react-router";
import { handleOAuthCallback } from "../services/oauth";
import { useAuthStore } from "../store/auth-store";

/**
 * OAuth callback page - handles the redirect from authorization server
 * Extracts code, exchanges for tokens, stores auth, and redirects to app
 */
export function OAuthCallbackPage() {
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);
  const [error, setError] = useState<string | null>(null);
  const hasProcessed = useRef(false);

  useEffect(() => {
    // Prevent double execution in React StrictMode
    if (hasProcessed.current) return;
    hasProcessed.current = true;

    async function handleCallback() {
      try {
        // Get full URL with query params
        const callbackUrl = window.location.href;

        // Exchange code for tokens
        const authResponse = await handleOAuthCallback(callbackUrl);

        // Decode JWT to get user info
        const payload = JSON.parse(
          atob(authResponse.accessToken.split(".")[1])
        );

        // Store auth in Zustand
        setAuth(
          {
            id: payload.sub || payload.userId,
            email: payload.email,
            displayName: payload.name || payload.email,
          },
          authResponse.accessToken,
          authResponse.refreshToken
        );

        // Redirect to bookmarks
        navigate({ to: "/bookmarks" });
      } catch (err) {
        console.error("OAuth callback error:", err);
        setError(err instanceof Error ? err.message : "Authentication failed");
      }
    }

    handleCallback();
  }, [navigate, setAuth]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="w-full max-w-md space-y-4 p-6">
          <div className="rounded-lg border border-red-200 bg-red-50 p-4">
            <h2 className="text-lg font-semibold text-red-900">
              Authentication Error
            </h2>
            <p className="mt-2 text-sm text-red-700">{error}</p>
          </div>
          <button
            onClick={() => navigate({ to: "/login" })}
            className="w-full rounded-lg bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
          >
            Back to Login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="text-center">
        <div className="mx-auto h-12 w-12 animate-spin rounded-full border-b-2 border-blue-600"></div>
        <p className="mt-4 text-gray-600">Completing authentication...</p>
      </div>
    </div>
  );
}
