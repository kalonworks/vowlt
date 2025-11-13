import { useEffect, useState, useRef } from "react";
import { useNavigate } from "@tanstack/react-router";
import { handleOAuthCallback } from "../services/oauth";
import { useAuthStore } from "../store/auth-store";

interface JwtPayload {
  sub?: string;
  userId?: string;
  email: string;
  name: string; // Now required - we always include it in JWT
}

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

    async function handleCallback(): Promise<void> {
      try {
        // Get full URL with query params
        const callbackUrl = window.location.href;

        // Exchange code for tokens
        const authResponse = await handleOAuthCallback(callbackUrl);

        // Decode JWT to get user info
        const payload = JSON.parse(
          atob(authResponse.accessToken.split(".")[1])
        ) as JwtPayload;

        // Store auth in Zustand
        setAuth(
          {
            id: payload.sub ?? payload.userId ?? "",
            email: payload.email,
            displayName: payload.name, // Use name claim directly
            createdAt: "", // Not in JWT, will be fetched if needed
            lastLoginAt: undefined,
          },
          authResponse.accessToken,
          authResponse.refreshToken
        );

        // Redirect to bookmarks
        await navigate({ to: "/bookmarks" });
      } catch (err) {
        console.error("OAuth callback error:", err);
        setError(err instanceof Error ? err.message : "Authentication failed");
      }
    }

    void handleCallback();
  }, [navigate, setAuth]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-red-600 mb-4">
            Authentication Failed
          </h1>
          <p className="text-gray-600 mb-4">{error}</p>
          <a
            href="/login"
            className="text-blue-600 hover:text-blue-500 underline"
          >
            Try again
          </a>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
        <p className="text-gray-600">Completing authentication...</p>
      </div>
    </div>
  );
}
