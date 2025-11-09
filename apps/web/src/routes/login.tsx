import { createFileRoute, redirect } from "@tanstack/react-router";
import { LoginPage } from "@/features/auth/components/login-page";

export const Route = createFileRoute("/login")({
  component: LoginPage,
  beforeLoad: ({ context }) => {
    // If already authenticated, redirect to bookmarks
    if (context.auth.isAuthenticated) {
      throw redirect({ to: "/bookmarks" });
    }
  },
});
