import { createFileRoute, redirect, Link } from "@tanstack/react-router";
import { useState } from "react";
import { SearchBar } from "@/features/search/components/search-bar";
import { SearchResults } from "@/features/search/components/search-results";
import { useSemanticSearch } from "@/features/search/hooks/use-semantic-search";
import { useLogout } from "@/features/auth/hooks";
import { useAuthStore } from "@/features/auth/store/auth-store";
import { Button } from "@/components/ui/button";

export const Route = createFileRoute("/search")({
  component: SearchPage,
  beforeLoad: ({ context }) => {
    if (!context.auth.isAuthenticated) {
      throw redirect({ to: "/login" });
    }
  },
});

function SearchPage() {
  const user = useAuthStore((state) => state.user);
  const logout = useLogout();
  const [query, setQuery] = useState("");

  const { data, isLoading, isError, error } = useSemanticSearch({
    query,
    limit: 20,
    minimumScore: 0.5,
  });

  const hasSearched = query.trim().length > 0;

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <nav className="border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center gap-8">
              <h1 className="text-xl font-bold">Vowlt</h1>

              {/* Navigation Links */}
              <div className="flex items-center gap-1">
                <Button variant="ghost" asChild className="font-medium">
                  <Link to="/bookmarks">Bookmarks</Link>
                </Button>
                <Button variant="ghost" asChild className="font-medium">
                  <Link to="/search">Search</Link>
                </Button>
              </div>
            </div>

            <div className="flex items-center gap-4">
              <span className="text-sm text-muted-foreground">
                {user?.email}
              </span>
              <Button
                onClick={() => logout.mutate()}
                disabled={logout.isPending}
                variant="ghost"
              >
                {logout.isPending ? "Logging out..." : "Logout"}
              </Button>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 sm:px-0">
          {/* Page Header */}
          <div className="mb-6">
            <h1 className="text-3xl font-bold tracking-tight mb-2">
              Search Bookmarks
            </h1>
            <p className="text-muted-foreground">
              Find bookmarks using AI-powered semantic search
            </p>
          </div>

          {/* Search Bar */}
          <div className="mb-8">
            <SearchBar
              onSearch={setQuery}
              isLoading={isLoading}
              placeholder="Search with natural language..."
            />
          </div>

          {/* Search Results */}
          <SearchResults
            data={data}
            isLoading={isLoading}
            isError={isError}
            error={error as Error}
            hasSearched={hasSearched}
          />
        </div>
      </main>
    </div>
  );
}
