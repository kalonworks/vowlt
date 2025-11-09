import { z } from "zod";

// Base bookmark schema with all validation rules
export const bookmarkSchema = z.object({
  url: z
    .string()
    .min(1, "URL is required")
    .url("Must be a valid URL")
    .max(2048, "URL is too long"),

  title: z.string().max(500, "Title is too long").optional(),

  description: z.string().max(2000, "Description is too long").optional(),

  tags: z
    .array(z.string().max(50, "Tag is too long"))
    .max(20, "Too many tags")
    .optional()
    .default([]),
});

export const createBookmarkSchema = bookmarkSchema;

export const updateBookmarkSchema = bookmarkSchema.partial();

export type BookmarkFormData = z.infer<typeof bookmarkSchema>;
export type CreateBookmarkData = z.infer<typeof createBookmarkSchema>;
export type UpdateBookmarkData = z.infer<typeof updateBookmarkSchema>;
