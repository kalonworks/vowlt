import { createFileRoute, redirect } from "@tanstack/react-router";
import { BookmarksPage } from "@/features/bookmarks/components/bookmarks-page";

export const Route = createFileRoute("/bookmarks")({
  component: BookmarksPage,
  beforeLoad: ({ context }) => {
    if (!context.auth.isAuthenticated) {
      throw redirect({ to: "/login" });
    }
  },
});
