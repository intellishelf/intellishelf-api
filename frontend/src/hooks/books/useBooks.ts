import { useQuery } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';
import type { GetBooksParams } from '../../types/book';

export const useBooks = (params?: GetBooksParams) => {
  return useQuery({
    queryKey: ['books', params],
    queryFn: () => booksApi.getBooks(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};
