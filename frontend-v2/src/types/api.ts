// Common API response types

// Paged result for list endpoints
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// API error response
export interface ApiError {
  code: string;
  message: string;
}

// Generic query params for list endpoints
export interface QueryParams {
  page?: number;
  pageSize?: number;
  orderBy?: string;
  ascending?: boolean;
}
