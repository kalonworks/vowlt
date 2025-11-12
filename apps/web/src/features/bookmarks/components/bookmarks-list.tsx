import { useBookmarks } from "../hooks/use-bookmarks";
import { BookmarkCard } from "./bookmark-card";
import { BookmarkSkeleton } from "./bookmark-skeleton";
import { EmptyBookmarks } from "./empty-bookmarks";
import { isAxiosError, type AxiosError } from "axios";
import type { ApiError } from "@/lib/api-client";

export function BookmarksList() {
  const { data, isLoading, isError, error } = useBookmarks();

  // Loading state - show skeleton cards
  if (isLoading) {
    return (
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <BookmarkSkeleton key={i} />
        ))}
      </div>
    );
  }

  // Error state
  if (isError) {
    const axiosError = error as AxiosError<ApiError>;
    const errorMessage = isAxiosError(axiosError)
      ? (axiosError.response?.data?.detail ?? axiosError.message)
      : (error?.message ?? "Unknown error");

    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <p className="text-destructive text-center mb-2">
          Failed to load bookmarks
        </p>
        <p className="text-sm text-muted-foreground">{errorMessage}</p>
      </div>
    );
  }

  // Empty state
  if (!data?.items || data.items.length === 0) {
    return <EmptyBookmarks />;
  }

  // Success state - show bookmark cards
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {data.items.map((bookmark) => (
        <BookmarkCard key={bookmark.id} bookmark={bookmark} />
      ))}
    </div>
  );
}
