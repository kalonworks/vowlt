export interface Bookmark {
  id: string;
  url: string;
  title: string;
  description?: string;
  notes?: string;
  fullText?: string;
  faviconUrl?: string;
  ogImageUrl?: string;
  domain?: string;
  createdAt: string;
  updatedAt: string;
  lastAccessedAt?: string;
  hasEmbedding: boolean;
}

export interface CreateBookmarkRequest {
  url: string;
  title: string;
  description?: string;
  notes?: string;
  fullText?: string;
}

export interface UpdateBookmarkRequest {
  title?: string;
  description?: string;
  notes?: string;
}

export interface BookmarksResponse {
  items: Bookmark[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
