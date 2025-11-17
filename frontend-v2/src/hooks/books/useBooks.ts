import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import type { PagedResult, Book, BooksQueryParams } from '@/types/book';

export const useBooks = (params: BooksQueryParams = {}) => {
  return useQuery({
    queryKey: ['books', params],
    queryFn: () => api.get<PagedResult<Book>>('/books', params),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};
