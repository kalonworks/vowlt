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
import React from "react";

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
        <Controller
          name="tags"
          control={control}
          render={({ field }) => {
            const handleInputChange = (
              e: React.ChangeEvent<HTMLInputElement>
            ) => {
              let value = e.target.value;

              // Replace space with ", " automatically
              if (value.endsWith(" ") && !value.endsWith(", ")) {
                value = value.slice(0, -1) + ", ";
              }

              e.target.value = value;
            };

            const convertToArray = (value: string) => {
              // Convert comma/space-separated string to array
              const tagsArray = value
                .split(/[,\s]+/) // Split by comma OR space
                .map((tag) => tag.trim())
                .filter((tag) => tag.length > 0);

              field.onChange(tagsArray);
            };

            const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
              convertToArray(e.target.value);
            };

            const handleKeyDown = (
              e: React.KeyboardEvent<HTMLInputElement>
            ) => {
              if (e.key === "Enter") {
                // Convert tags before form submission
                convertToArray(e.currentTarget.value);
              }
            };

            return (
              <Input
                id="tags"
                placeholder="technology react frontend"
                defaultValue={field.value?.join(", ") || ""}
                onChange={handleInputChange}
                onBlur={handleBlur}
                onKeyDown={handleKeyDown}
                disabled={isSubmitting}
              />
            );
          }}
        />
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
