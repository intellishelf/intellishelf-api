import { apiClient } from '../client';
import type {
  Book,
  PagedResult,
  GetBooksParams,
  SearchBooksParams,
  AddBookRequest,
} from '../../types/book';
import { buildBookFormData } from '../../utils/formData';

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
    const formData = buildBookFormData(data);

    const response = await apiClient.post<Book>('/books', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  // Update book
  updateBook: async (bookId: string, data: AddBookRequest): Promise<void> => {
    const formData = buildBookFormData(data);

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
