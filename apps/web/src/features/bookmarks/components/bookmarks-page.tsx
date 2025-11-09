import { useLogout } from "@/features/auth/hooks";
import { useAuthStore } from "@/features/auth/store/auth-store";

export const BookmarksPage = () => {
  const user = useAuthStore((state) => state.user);
  const logout = useLogout();

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-bold">Vowlt</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-700">{user?.email}</span>
              <button
                onClick={() => logout.mutate()}
                disabled={logout.isPending}
                className="px-4 py-2 text-sm font-medium text-gray-700 hover:text-gray-900"
              >
                {logout.isPending ? "Logging out..." : "Logout"}
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <h2 className="text-2xl font-bold mb-4">Your Bookmarks</h2>
          <p className="text-gray-600">
            Bookmark management coming soon! Authentication is working. 🎉
          </p>
        </div>
      </main>
    </div>
  );
};
