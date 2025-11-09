import { apiClient } from "@/lib/api-client";
import type { SearchRequest, SearchResponse } from "../types";

export const searchApi = {
  // Semantic search using AI embeddings
  semanticSearch: async (request: SearchRequest): Promise<SearchResponse> => {
    const response = await apiClient.post<SearchResponse>(
      "/api/search",
      request
    );
    return response.data;
  },

  // Find similar bookmarks to a given bookmark
  findSimilar: async (
    bookmarkId: string,
    limit: number = 10
  ): Promise<SearchResponse> => {
    const response = await apiClient.get<SearchResponse>(
      `/api/search/similar/${bookmarkId}`,
      {
        params: { limit },
      }
    );
    return response.data;
  },
};
