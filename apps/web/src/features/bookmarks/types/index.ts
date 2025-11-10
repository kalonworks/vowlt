// Core bookmark type returned from API
export interface Bookmark {
  id: string;
  userId: string;
  url: string;
  title?: string;
  description?: string;
  tags: string[];
  generatedTags: string[];
  createdAt: string;
  updatedAt: string;
}

// Request types for API calls
export interface CreateBookmarkRequest {
  url: string;
  title?: string;
  description?: string;
  tags?: string[];
}

export interface UpdateBookmarkRequest {
  url?: string;
  title?: string;
  description?: string;
  tags?: string[];
}

// Query parameters for fetching bookmarks
export interface GetBookmarksParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  tags?: string[];
}
