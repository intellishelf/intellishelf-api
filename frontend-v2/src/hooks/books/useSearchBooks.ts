import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import type { PagedResult, Book, SearchQueryParams } from '@/types/book';

export const useSearchBooks = (params: SearchQueryParams) => {
  return useQuery({
    queryKey: ['books', 'search', params],
    queryFn: () => api.get<PagedResult<Book>>('/books/search', params),
    enabled: params.searchTerm.length > 0 || params.status !== null,
    staleTime: 30 * 1000, // 30 seconds
  });
};
