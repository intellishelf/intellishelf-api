import { apiClient } from '../client';
import type {
  Book,
  PagedResult,
  GetBooksParams,
  SearchBooksParams,
  AddBookRequest,
} from '../../types/book';

export const booksApi = {
  // Get paginated books
  getBooks: async (params?: GetBooksParams): Promise<PagedResult<Book>> => {
    const response = await apiClient.get<PagedResult<Book>>('/books', { params });
    return response.data;
  },

  // Get all books (no pagination)
  getAllBooks: async (): Promise<Book[]> => {
    const response = await apiClient.get<Book[]>('/books/all');
    return response.data;
  },

  // Get single book by ID
  getBook: async (bookId: string): Promise<Book> => {
    const response = await apiClient.get<Book>(`/books/${bookId}`);
    return response.data;
  },

  // Search books
  searchBooks: async (params: SearchBooksParams): Promise<PagedResult<Book>> => {
    const response = await apiClient.get<PagedResult<Book>>('/books/search', { params });
    return response.data;
  },

  // Add new book
  addBook: async (data: AddBookRequest): Promise<Book> => {
    const formData = new FormData();

    // Append all fields to FormData
    formData.append('title', data.title);
    if (data.annotation) formData.append('annotation', data.annotation);
    if (data.authors) formData.append('authors', data.authors);
    if (data.description) formData.append('description', data.description);
    if (data.isbn10) formData.append('isbn10', data.isbn10);
    if (data.isbn13) formData.append('isbn13', data.isbn13);
    if (data.pages !== undefined) formData.append('pages', data.pages.toString());
    if (data.publicationDate) formData.append('publicationDate', data.publicationDate);
    if (data.publisher) formData.append('publisher', data.publisher);
    if (data.tags) {
      data.tags.forEach((tag) => formData.append('tags', tag));
    }
    if (data.imageFile) formData.append('imageFile', data.imageFile);
    if (data.status !== undefined) formData.append('status', data.status.toString());
    if (data.startedReadingDate) formData.append('startedReadingDate', data.startedReadingDate);
    if (data.finishedReadingDate) formData.append('finishedReadingDate', data.finishedReadingDate);

    const response = await apiClient.post<Book>('/books', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  // Update book
  updateBook: async (bookId: string, data: AddBookRequest): Promise<void> => {
    const formData = new FormData();

    // Append all fields to FormData
    formData.append('title', data.title);
    if (data.annotation) formData.append('annotation', data.annotation);
    if (data.authors) formData.append('authors', data.authors);
    if (data.description) formData.append('description', data.description);
    if (data.isbn10) formData.append('isbn10', data.isbn10);
    if (data.isbn13) formData.append('isbn13', data.isbn13);
    if (data.pages !== undefined) formData.append('pages', data.pages.toString());
    if (data.publicationDate) formData.append('publicationDate', data.publicationDate);
    if (data.publisher) formData.append('publisher', data.publisher);
    if (data.tags) {
      data.tags.forEach((tag) => formData.append('tags', tag));
    }
    if (data.imageFile) formData.append('imageFile', data.imageFile);
    if (data.status !== undefined) formData.append('status', data.status.toString());
    if (data.startedReadingDate) formData.append('startedReadingDate', data.startedReadingDate);
    if (data.finishedReadingDate) formData.append('finishedReadingDate', data.finishedReadingDate);

    await apiClient.put(`/books/${bookId}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  // Delete book
  deleteBook: async (bookId: string): Promise<void> => {
    await apiClient.delete(`/books/${bookId}`);
  },
};
