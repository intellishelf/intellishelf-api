import { useQuery, type UseQueryOptions } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';
import type { GetBooksParams, PagedResult, Book } from '../../types/book';

export const useBooks = (
  params?: GetBooksParams,
  options?: Omit<UseQueryOptions<PagedResult<Book>>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: ['books', params],
    queryFn: () => booksApi.getBooks(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
    ...options,
  });
};
