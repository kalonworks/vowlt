import type { Bookmark } from "@/features/bookmarks/types";

export interface SearchRequest {
  query: string;
  limit?: number;
  similarityThreshold?: number;
}

export interface SearchResult {
  bookmark: Bookmark;
  similarity: number;
  rank: number;
}

export interface SearchResponse {
  results: SearchResult[];
  totalCount: number;
  queryProcessingTimeMs: number;
}
