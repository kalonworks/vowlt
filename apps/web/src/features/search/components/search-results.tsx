import { SearchX, Sparkles } from "lucide-react";
import { SearchResultCard } from "./search-result-card";
import { Skeleton } from "@/components/ui/skeleton";
import type { SearchResponse } from "../types";

interface SearchResultsProps {
  data?: SearchResponse;
  isLoading: boolean;
  isError: boolean;
  error?: Error;
  hasSearched: boolean;
}

export function SearchResults({
  data,
  isLoading,
  isError,
  error,
  hasSearched,
}: SearchResultsProps) {
  // Initial state - no search yet
  if (!hasSearched) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <Sparkles className="h-16 w-16 text-muted-foreground mb-4" />
        <h3 className="text-xl font-semibold mb-2">AI-Powered Search</h3>
        <p className="text-muted-foreground text-center max-w-md">
          Search your bookmarks using natural language. Our AI understands
          meaning, not just keywords.
        </p>
      </div>
    );
  }

  // Loading state
  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <div className="h-4 w-4 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          Searching with AI...
        </div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="space-y-3 p-6 border rounded-lg">
              <Skeleton className="h-6 w-3/4" />
              <Skeleton className="h-3 w-1/2" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-2/3" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  // Error state
  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <p className="text-destructive text-center mb-2">Search failed</p>
        <p className="text-sm text-muted-foreground">
          {error?.message || "Unknown error"}
        </p>
      </div>
    );
  }

  // No results
  if (!data?.results || data.results.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <SearchX className="h-16 w-16 text-muted-foreground mb-4" />
        <h3 className="text-xl font-semibold mb-2">No results found</h3>
        <p className="text-muted-foreground text-center max-w-md">
          Try a different search query or adjust your filters.
        </p>
      </div>
    );
  }

  // Success - show results
  return (
    <div className="space-y-4">
      {/* Search metadata */}
      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <span>
          Found {data.totalResults} result{data.totalResults !== 1 ? "s" : ""}
        </span>
        <span>Search took {data.processingTimeMs.toFixed(0)}ms</span>
      </div>

      {/* Results grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {data.results.map((result) => (
          <SearchResultCard key={result.id} result={result} />
        ))}
      </div>
    </div>
  );
}
