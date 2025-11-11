import { useMutation, useQueryClient } from "@tanstack/react-query";
import { bookmarksApi } from "../api/bookmarks";
import type { CreateBookmarkRequest, Bookmark } from "../types";
import type { PaginatedResponse } from "@/types/api";

// Define the context type for type safety
interface CreateBookmarkContext {
  previousBookmarks: PaginatedResponse<Bookmark> | undefined;
}

export const useCreateBookmark = () => {
  const queryClient = useQueryClient();

  return useMutation<
    Bookmark, // TData - success response type
    Error, // TError - error type
    CreateBookmarkRequest, // TVariables - mutation input type
    CreateBookmarkContext // TContext - context type from onMutate
  >({
    mutationFn: bookmarksApi.createBookmark,

    onMutate: async (newBookmark) => {
      await queryClient.cancelQueries({ queryKey: ["bookmarks"] });

      const previousBookmarks = queryClient.getQueryData<
        PaginatedResponse<Bookmark>
      >(["bookmarks", {}]);

      if (previousBookmarks) {
        const optimisticBookmark: Bookmark = {
          id: `temp-${Date.now()}`,
          userId: "",
          ...newBookmark,
          tags: newBookmark.tags || [],
          generatedTags: [],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        };

        queryClient.setQueryData<PaginatedResponse<Bookmark>>(
          ["bookmarks", {}],
          {
            ...previousBookmarks,
            items: [optimisticBookmark, ...previousBookmarks.items],
            totalCount: previousBookmarks.totalCount + 1,
          }
        );
      }

      return { previousBookmarks };
    },

    onError: (_error, _newBookmark, context) => {
      if (context?.previousBookmarks) {
        queryClient.setQueryData(["bookmarks", {}], context.previousBookmarks);
      }
    },

    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: ["bookmarks"] });
    },
  });
};
