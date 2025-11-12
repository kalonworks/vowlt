import { useState } from "react";
import { Pencil } from "lucide-react";
import { toast } from "sonner";
import { isAxiosError, type AxiosError } from "axios";
import type { ApiError } from "@/lib/api-client";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { BookmarkForm } from "./bookmark-form";
import { useUpdateBookmark } from "../hooks/use-update-bookmark";
import type { Bookmark } from "../types";
import type { BookmarkFormData } from "../schemas/bookmark-schema";

interface EditBookmarkDialogProps {
  bookmark: Bookmark;
}

export function EditBookmarkDialog({ bookmark }: EditBookmarkDialogProps) {
  const [open, setOpen] = useState(false);
  const updateBookmark = useUpdateBookmark();

  const handleSubmit = (data: BookmarkFormData) => {
    updateBookmark.mutate(
      { id: bookmark.id, data },
      {
        onSuccess: () => {
          toast.success("Bookmark updated successfully!");
          setOpen(false);
        },
        onError: (error) => {
          const axiosError = error as AxiosError<ApiError>;
          const message = isAxiosError(axiosError)
            ? (axiosError.response?.data?.detail ?? "Failed to update bookmark")
            : "Failed to update bookmark";
          toast.error(message);
        },
      }
    );
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <Pencil className="h-4 w-4" />
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[525px]">
        <DialogHeader>
          <DialogTitle>Edit Bookmark</DialogTitle>
          <DialogDescription>Update your bookmark details.</DialogDescription>
        </DialogHeader>
        <BookmarkForm
          defaultValues={{
            url: bookmark.url,
            title: bookmark.title,
            description: bookmark.description,
            tags: bookmark.tags,
          }}
          onSubmit={handleSubmit}
          isSubmitting={updateBookmark.isPending}
          submitLabel="Update"
        />
      </DialogContent>
    </Dialog>
  );
}
