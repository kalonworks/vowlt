import { useState } from "react";
import { Plus } from "lucide-react";
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
import { useCreateBookmark } from "../hooks/use-create-bookmark";
import type { BookmarkFormData } from "../schemas/bookmark-schema";

export function CreateBookmarkDialog() {
  const [open, setOpen] = useState(false);
  const createBookmark = useCreateBookmark();

  const handleSubmit = (data: BookmarkFormData) => {
    createBookmark.mutate(data, {
      onSuccess: () => {
        toast.success("Bookmark created successfully!");
        setOpen(false);
      },
      onError: (error) => {
        const axiosError = error as AxiosError<ApiError>;
        const message = isAxiosError(axiosError)
          ? (axiosError.response?.data?.detail ?? "Failed to create bookmark")
          : "Failed to create bookmark";
        toast.error(message);
      },
    });
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>
          <Plus className="h-4 w-4 mr-2" />
          Add Bookmark
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[525px]">
        <DialogHeader>
          <DialogTitle>Create Bookmark</DialogTitle>
          <DialogDescription>
            Add a new bookmark to your collection.
          </DialogDescription>
        </DialogHeader>
        <BookmarkForm
          onSubmit={handleSubmit}
          isSubmitting={createBookmark.isPending}
          submitLabel="Create"
        />
      </DialogContent>
    </Dialog>
  );
}
