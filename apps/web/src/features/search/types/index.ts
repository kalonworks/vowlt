// Search request to backend
export interface SearchRequest {
  query: string;
  limit?: number;
  minimumScore?: number;
  fromDate?: string;
  toDate?: string;
  domain?: string;
}

// Individual search result
export interface SearchResult {
  id: string;
  url: string;
  title: string;
  description?: string;
  domain?: string;
  createdAt: string;
  similarityScore: number; // 0-1, rounded to 4 decimals
}

// Search response from backend
export interface SearchResponse {
  query: string;
  results: SearchResult[];
  totalResults: number;
  processingTimeMs: number;
}
