import { createFileRoute, redirect } from "@tanstack/react-router";
import { BookmarksPage } from "@/features/bookmarks/components/bookmarks-page";

export const Route = createFileRoute("/bookmarks")({
  component: BookmarksPage,
  beforeLoad: ({ context }) => {
    console.log("Bookmarks beforeLoad - context.auth:", context.auth); // DEBUG

    // Use router context (always fresh!)
    if (!context.auth.isAuthenticated) {
      console.log("Not authenticated, redirecting to login"); // DEBUG
      throw redirect({ to: "/login" });
    }
    console.log("Authenticated, allowing access"); // DEBUG
  },
});
