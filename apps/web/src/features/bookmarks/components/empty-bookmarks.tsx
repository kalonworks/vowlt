import { BookmarkX } from "lucide-react";
import { CreateBookmarkDialog } from "./create-bookmark-dialog";

export function EmptyBookmarks() {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-4">
      <BookmarkX className="h-16 w-16 text-muted-foreground mb-4" />
      <h3 className="text-xl font-semibold mb-2">No bookmarks yet</h3>
      <p className="text-muted-foreground text-center mb-6 max-w-md">
        Start building your collection by adding your first bookmark.
      </p>
      <CreateBookmarkDialog />
    </div>
  );
}
