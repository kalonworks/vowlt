import { useState } from "react";
import { Trash2 } from "lucide-react";
import { toast } from "sonner";
import { isAxiosError, type AxiosError } from "axios";
import type { ApiError } from "@/lib/api-client";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { useDeleteBookmark } from "../hooks/use-delete-bookmark";

interface DeleteBookmarkDialogProps {
  bookmarkId: string;
  bookmarkTitle?: string;
}

export function DeleteBookmarkDialog({
  bookmarkId,
  bookmarkTitle,
}: DeleteBookmarkDialogProps) {
  const [open, setOpen] = useState(false);
  const deleteBookmark = useDeleteBookmark();

  const handleDelete = () => {
    deleteBookmark.mutate(bookmarkId, {
      onSuccess: () => {
        toast.success("Bookmark deleted successfully!");
        setOpen(false);
      },
      onError: (error) => {
        const axiosError = error as AxiosError<ApiError>;
        const message = isAxiosError(axiosError)
          ? (axiosError.response?.data?.detail ?? "Failed to delete bookmark")
          : "Failed to delete bookmark";
        toast.error(message);
      },
    });
  };

  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <AlertDialogTrigger asChild>
        <Button variant="outline" size="sm">
          <Trash2 className="h-4 w-4" />
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Bookmark</AlertDialogTitle>
          <AlertDialogDescription>
            Are you sure you want to delete{" "}
            {bookmarkTitle ? `"${bookmarkTitle}"` : "this bookmark"}? This
            action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteBookmark.isPending}>
            Cancel
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault();
              handleDelete();
            }}
            disabled={deleteBookmark.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {deleteBookmark.isPending ? "Deleting..." : "Delete"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
