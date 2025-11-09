import { useMutation, useQueryClient } from "@tanstack/react-query";
import { bookmarksApi } from "../api/bookmarks";
import type { UpdateBookmarkRequest, Bookmark } from "../types";
import type { PaginatedResponse } from "@/types/api";

interface UpdateBookmarkContext {
  previousBookmarks: PaginatedResponse<Bookmark> | undefined;
}

export const useUpdateBookmark = () => {
  const queryClient = useQueryClient();

  return useMutation<
    Bookmark, // TData
    Error, // TError
    { id: string; data: UpdateBookmarkRequest }, // TVariables
    UpdateBookmarkContext // TContext
  >({
    mutationFn: ({ id, data }) => bookmarksApi.updateBookmark(id, data),

    onMutate: async ({ id, data }) => {
      await queryClient.cancelQueries({ queryKey: ["bookmarks"] });

      const previousBookmarks = queryClient.getQueryData<
        PaginatedResponse<Bookmark>
      >(["bookmarks", {}]);

      if (previousBookmarks) {
        queryClient.setQueryData<PaginatedResponse<Bookmark>>(
          ["bookmarks", {}],
          {
            ...previousBookmarks,
            items: previousBookmarks.items.map((bookmark) =>
              bookmark.id === id
                ? {
                    ...bookmark,
                    ...data,
                    tags: data.tags ?? bookmark.tags,
                    updatedAt: new Date().toISOString(),
                  }
                : bookmark
            ),
          }
        );
      }

      return { previousBookmarks };
    },

    onError: (_error, _variables, context) => {
      if (context?.previousBookmarks) {
        queryClient.setQueryData(["bookmarks", {}], context.previousBookmarks);
      }
    },

    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: ["bookmarks"] });
    },
  });
};
