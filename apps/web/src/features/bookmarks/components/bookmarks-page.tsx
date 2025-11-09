import { useLogout } from "@/features/auth/hooks";
import { useAuthStore } from "@/features/auth/store/auth-store";
import { Button } from "@/components/ui/button";
import { CreateBookmarkDialog } from "./create-bookmark-dialog";
import { BookmarksList } from "./bookmarks-list";

export const BookmarksPage = () => {
  const user = useAuthStore((state) => state.user);
  const logout = useLogout();

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <nav className="border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-bold">Vowlt</h1>
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
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-3xl font-bold tracking-tight">
                Your Bookmarks
              </h2>
              <p className="text-muted-foreground mt-1">
                Manage your saved bookmarks
              </p>
            </div>
            <CreateBookmarkDialog />
          </div>

          {/* Bookmarks List */}
          <BookmarksList />
        </div>
      </main>
    </div>
  );
};
