import { createFileRoute, redirect } from "@tanstack/react-router";
import { OAuthCallbackPage } from "@/features/auth/components/oauth-callback-page";

export const Route = createFileRoute("/auth/callback")({
  component: OAuthCallbackPage,
  beforeLoad: ({ context }) => {
    // If already authenticated, redirect to bookmarks
    if (context.auth.isAuthenticated) {
      throw redirect({ to: "/bookmarks" });
    }
  },
});
