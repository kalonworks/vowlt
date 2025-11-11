import { useQuery } from "@tanstack/react-query";
import { searchApi } from "../api/search";
import type { SearchRequest } from "../types";

export const useSemanticSearch = (request: SearchRequest) => {
  const { query, ...filters } = request;

  // Match backend validation: query must be 3-500 characters
  const trimmedQuery = query.trim();
  const isValidQuery = trimmedQuery.length >= 3 && trimmedQuery.length <= 500;

  return useQuery({
    queryKey: ["search", "semantic", trimmedQuery, filters],
    queryFn: () =>
      searchApi.semanticSearch({
        query: trimmedQuery, // Send trimmed query
        ...filters,
      }),
    // Only run query if valid (2-500 chars)
    enabled: isValidQuery,
    // Cache results for 5 minutes
    staleTime: 1000 * 60 * 5,
    // Don't retry on error (search is expensive)
    retry: false,
  });
};
