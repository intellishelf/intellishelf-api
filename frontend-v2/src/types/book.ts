// Reading status enum matching backend
export enum ReadingStatus {
  Unread = 0,
  Reading = 1,
  Read = 2,
}

// Book model matching backend API
export interface Book {
  id: string;
  createdDate: string;
  title: string;
  userId: string;
  annotation?: string | null;
  authors?: string | null; // Comma-separated string from backend
  description?: string | null;
  coverImageUrl?: string | null;
  isbn10?: string | null;
  isbn13?: string | null;
  pages?: number | null;
  publicationDate?: string | null;
  publisher?: string | null;
  tags?: string[] | null;
  status: ReadingStatus;
  startedReadingDate?: string | null;
  finishedReadingDate?: string | null;
}

// Paged result from backend
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Book order by enum
export enum BookOrderBy {
  Title = 0,
  Author = 1,
  Published = 2,
  Added = 3,
}

// Query parameters for books list
export interface BooksQueryParams {
  page?: number;
  pageSize?: number;
  orderBy?: BookOrderBy;
  ascending?: boolean;
}

// Search query parameters
export interface SearchQueryParams {
  searchTerm: string;
  page?: number;
  pageSize?: number;
  status?: ReadingStatus | null;
}

// Form data for adding/editing books (before converting to FormData)
export interface BookFormData {
  title: string;
  annotation?: string;
  authors?: string; // Comma-separated in form, will be split to array for backend
  description?: string;
  isbn10?: string;
  isbn13?: string;
  pages?: number;
  publicationDate?: string;
  publisher?: string;
  tags?: string; // Comma-separated in form, will be split to array for backend
  imageFile?: File;
  status?: ReadingStatus;
  startedReadingDate?: string;
  finishedReadingDate?: string;
}
