export const ReadingStatus = {
  Unread: 0,
  Reading: 1,
  Read: 2,
} as const;

export type ReadingStatus = typeof ReadingStatus[keyof typeof ReadingStatus];

export const BookOrderBy = {
  Title: 0,
  Author: 1,
  Published: 2,
  Added: 3,
} as const;

export type BookOrderBy = typeof BookOrderBy[keyof typeof BookOrderBy];

export interface Book {
  id: string;
  createdDate: string;
  title: string;
  userId: string;
  annotation?: string;
  authors?: string;
  description?: string;
  coverImageUrl?: string;
  isbn10?: string;
  isbn13?: string;
  pages?: number;
  publicationDate?: string;
  publisher?: string;
  tags?: string[];
  status: ReadingStatus;
  startedReadingDate?: string;
  finishedReadingDate?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetBooksParams {
  page?: number;
  pageSize?: number;
  orderBy?: BookOrderBy;
  ascending?: boolean;
}

export interface SearchBooksParams {
  searchTerm: string;
  page?: number;
  pageSize?: number;
  status?: ReadingStatus;
}

export interface AddBookRequest {
  title: string;
  annotation?: string;
  authors?: string;
  description?: string;
  isbn10?: string;
  isbn13?: string;
  pages?: number;
  publicationDate?: string;
  publisher?: string;
  tags?: string[];
  imageFile?: File;
  status?: ReadingStatus;
  startedReadingDate?: string;
  finishedReadingDate?: string;
}

export interface UpdateBookRequest extends AddBookRequest {
  id: string;
}
