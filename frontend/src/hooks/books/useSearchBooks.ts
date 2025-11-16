import { useQuery, type UseQueryOptions } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';
import type { SearchBooksParams, PagedResult, Book } from '../../types/book';

export const useSearchBooks = (
  params: SearchBooksParams,
  options?: Omit<UseQueryOptions<PagedResult<Book>>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: ['books', 'search', params],
    queryFn: () => booksApi.searchBooks(params),
    staleTime: 2 * 60 * 1000, // 2 minutes
    ...options,
  });
};
