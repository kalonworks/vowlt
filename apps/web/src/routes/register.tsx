import { createFileRoute, redirect } from "@tanstack/react-router";
import { RegisterPage } from "@/features/auth/components/register-page";

export const Route = createFileRoute("/register")({
  component: RegisterPage,
  beforeLoad: ({ context }) => {
    const isAuthenticated = context.auth.isAuthenticated;
    if (isAuthenticated) {
      throw redirect({ to: "/bookmarks" });
    }
  },
});
