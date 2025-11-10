import { formatDistanceToNow } from "date-fns";
import { ExternalLink } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { EditBookmarkDialog } from "./edit-bookmark-dialog";
import { DeleteBookmarkDialog } from "./delete-bookmark-dialog";
import type { Bookmark } from "../types";

interface BookmarkCardProps {
  bookmark: Bookmark;
}

export function BookmarkCard({ bookmark }: BookmarkCardProps) {
  const displayTitle = bookmark.title || bookmark.url;
  const timeAgo = formatDistanceToNow(new Date(bookmark.createdAt), {
    addSuffix: true,
  });

  return (
    <Card className="h-full flex flex-col">
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-lg line-clamp-2">
              <a
                href={bookmark.url}
                target="_blank"
                rel="noopener noreferrer"
                className="hover:underline inline-flex items-center gap-1"
              >
                {displayTitle}
                <ExternalLink className="h-4 w-4 flex-shrink-0" />
              </a>
            </CardTitle>
            {bookmark.title && (
              <CardDescription className="mt-1 text-xs truncate">
                {bookmark.url}
              </CardDescription>
            )}
          </div>
        </div>
      </CardHeader>

      {bookmark.description && (
        <CardContent className="flex-1">
          <p className="text-sm text-muted-foreground line-clamp-3">
            {bookmark.description}
          </p>
        </CardContent>
      )}

      <CardFooter className="flex flex-col items-start gap-3">
        {/* User Tags */}
        {bookmark.tags.length > 0 && (
          <div className="flex flex-wrap gap-1 w-full">
            {bookmark.tags.map((tag) => (
              <span
                key={tag}
                className="inline-flex items-center px-2 py-0.5 text-xs bg-primary/10 text-primary border border-primary/20 
  rounded-md font-medium"
              >
                {tag}
              </span>
            ))}
          </div>
        )}

        {/* AI-Generated Tags */}
        {bookmark.generatedTags.length > 0 && (
          <div className="flex flex-wrap gap-1 w-full">
            {bookmark.generatedTags.map((tag) => (
              <span
                key={tag}
                className="inline-flex items-center gap-1 px-2 py-0.5 text-xs bg-secondary text-secondary-foreground 
  rounded-md opacity-75"
              >
                <span className="text-[10px]">✨</span>
                {tag}
              </span>
            ))}
          </div>
        )}

        {/* Actions & Timestamp */}
        <div className="flex items-center justify-between w-full">
          <span className="text-xs text-muted-foreground">Saved {timeAgo}</span>
          <div className="flex items-center gap-2">
            <EditBookmarkDialog bookmark={bookmark} />
            <DeleteBookmarkDialog
              bookmarkId={bookmark.id}
              bookmarkTitle={bookmark.title}
            />
          </div>
        </div>
      </CardFooter>
    </Card>
  );
}
