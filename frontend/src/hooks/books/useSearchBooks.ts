import { useQuery } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';
import type { SearchBooksParams } from '../../types/book';

export const useSearchBooks = (params: SearchBooksParams) => {
  return useQuery({
    queryKey: ['books', 'search', params],
    queryFn: () => booksApi.searchBooks(params),
    enabled: !!params.searchTerm,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};
