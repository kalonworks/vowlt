import { apiClient } from "@/lib/api-client";
import type { PaginatedResponse } from "@/types/api";
import type {
  Bookmark,
  CreateBookmarkRequest,
  UpdateBookmarkRequest,
  GetBookmarksParams,
} from "../types";

export const bookmarksApi = {
  // Get paginated bookmarks
  getBookmarks: async (
    params: GetBookmarksParams = {}
  ): Promise<PaginatedResponse<Bookmark>> => {
    const { pageNumber = 1, pageSize = 20, search, tags } = params;

    const response = await apiClient.get<PaginatedResponse<Bookmark>>(
      "/api/bookmarks",
      {
        params: {
          pageNumber,
          pageSize,
          search,
          tags: tags?.join(","), // Convert array to comma-separated string
        },
      }
    );

    return response.data;
  },
  createBookmark: async (data: CreateBookmarkRequest): Promise<Bookmark> => {
    const response = await apiClient.post<Bookmark>("/api/bookmarks", data);
    return response.data;
  },
  updateBookmark: async (
    id: string,
    data: UpdateBookmarkRequest
  ): Promise<Bookmark> => {
    const response = await apiClient.put<Bookmark>(
      `/api/bookmarks/${id}`,
      data
    );
    return response.data;
  },
  deleteBookmark: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/bookmarks/${id}`);
  },
};
