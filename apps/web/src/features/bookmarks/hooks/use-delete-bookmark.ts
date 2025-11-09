import { useMutation, useQueryClient } from "@tanstack/react-query";
import { bookmarksApi } from "../api/bookmarks";
import type { Bookmark } from "../types";
import type { PaginatedResponse } from "@/types/api";

interface DeleteBookmarkContext {
  previousBookmarks: PaginatedResponse<Bookmark> | undefined;
}

export const useDeleteBookmark = () => {
  const queryClient = useQueryClient();

  return useMutation<
    void, // TData - delete returns nothing
    Error, // TError
    string, // TVariables - just the bookmark ID
    DeleteBookmarkContext // TContext
  >({
    mutationFn: bookmarksApi.deleteBookmark,

    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: ["bookmarks"] });

      const previousBookmarks = queryClient.getQueryData<
        PaginatedResponse<Bookmark>
      >(["bookmarks", {}]);

      if (previousBookmarks) {
        queryClient.setQueryData<PaginatedResponse<Bookmark>>(
          ["bookmarks", {}],
          {
            ...previousBookmarks,
            items: previousBookmarks.items.filter(
              (bookmark) => bookmark.id !== id
            ),
            totalCount: previousBookmarks.totalCount - 1,
          }
        );
      }

      return { previousBookmarks };
    },

    onError: (_error, _id, context) => {
      if (context?.previousBookmarks) {
        queryClient.setQueryData(["bookmarks", {}], context.previousBookmarks);
      }
    },

    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: ["bookmarks"] });
    },
  });
};
