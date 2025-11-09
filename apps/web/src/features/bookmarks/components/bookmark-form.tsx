import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { X } from "lucide-react";
import {
  bookmarkSchema,
  type BookmarkFormData,
} from "../schemas/bookmark-schema";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";

interface BookmarkFormProps {
  defaultValues?: Partial<BookmarkFormData>;
  onSubmit: (data: BookmarkFormData) => void;
  isSubmitting?: boolean;
  submitLabel?: string;
}

export function BookmarkForm({
  defaultValues,
  onSubmit,
  isSubmitting = false,
  submitLabel = "Save",
}: BookmarkFormProps) {
  const {
    control,
    handleSubmit,
    formState: { errors },
    watch,
    setValue,
  } = useForm<BookmarkFormData>({
    resolver: zodResolver(bookmarkSchema),
    defaultValues: {
      url: defaultValues?.url || "",
      title: defaultValues?.title || "",
      description: defaultValues?.description || "",
      tags: defaultValues?.tags || [],
    },
  });

  const tags = watch("tags") || [];

  const handleAddTag = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const input = e.currentTarget;
      const newTag = input.value.trim();

      if (newTag && !tags.includes(newTag) && tags.length < 20) {
        setValue("tags", [...tags, newTag]);
        input.value = "";
      }
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    setValue(
      "tags",
      tags.filter((tag) => tag !== tagToRemove)
    );
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {/* URL Field */}
      <div className="space-y-2">
        <Label htmlFor="url">
          URL <span className="text-destructive">*</span>
        </Label>
        <Controller
          name="url"
          control={control}
          render={({ field }) => (
            <Input
              {...field}
              id="url"
              type="url"
              placeholder="https://example.com"
              disabled={isSubmitting}
            />
          )}
        />
        {errors.url && (
          <p className="text-sm text-destructive">{errors.url.message}</p>
        )}
      </div>

      {/* Title Field */}
      <div className="space-y-2">
        <Label htmlFor="title">Title</Label>
        <Controller
          name="title"
          control={control}
          render={({ field }) => (
            <Input
              {...field}
              id="title"
              placeholder="My awesome bookmark"
              disabled={isSubmitting}
            />
          )}
        />
        {errors.title && (
          <p className="text-sm text-destructive">{errors.title.message}</p>
        )}
      </div>

      {/* Description Field */}
      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Controller
          name="description"
          control={control}
          render={({ field }) => (
            <Textarea
              {...field}
              id="description"
              placeholder="What's this bookmark about?"
              rows={3}
              disabled={isSubmitting}
            />
          )}
        />
        {errors.description && (
          <p className="text-sm text-destructive">
            {errors.description.message}
          </p>
        )}
      </div>

      {/* Tags Field */}
      <div className="space-y-2">
        <Label htmlFor="tags">Tags</Label>
        <Input
          id="tags"
          placeholder="Type a tag and press Enter"
          onKeyDown={handleAddTag}
          disabled={isSubmitting}
        />
        {tags.length > 0 && (
          <div className="flex flex-wrap gap-2 mt-2">
            {tags.map((tag) => (
              <span
                key={tag}
                className="inline-flex items-center gap-1 px-2 py-1 text-sm bg-secondary text-secondary-foreground 
  rounded-md"
              >
                {tag}
                <button
                  type="button"
                  onClick={() => handleRemoveTag(tag)}
                  disabled={isSubmitting}
                  className="hover:text-destructive"
                >
                  <X className="h-3 w-3" />
                </button>
              </span>
            ))}
          </div>
        )}
        {errors.tags && (
          <p className="text-sm text-destructive">{errors.tags.message}</p>
        )}
      </div>

      {/* Submit Button */}
      <Button type="submit" disabled={isSubmitting} className="w-full">
        {isSubmitting ? "Saving..." : submitLabel}
      </Button>
    </form>
  );
}
