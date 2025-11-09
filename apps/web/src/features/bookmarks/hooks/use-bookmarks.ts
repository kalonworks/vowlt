import { useQuery } from "@tanstack/react-query";
import { bookmarksApi } from "../api/bookmarks";
import type { GetBookmarksParams } from "../types";

export const useBookmarks = (params: GetBookmarksParams = {}) => {
  return useQuery({
    queryKey: ["bookmarks", params],
    queryFn: () => bookmarksApi.getBookmarks(params),
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
};
