import { useQuery } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';

export const useBook = (bookId: string) => {
  return useQuery({
    queryKey: ['books', bookId],
    queryFn: () => booksApi.getBook(bookId),
    enabled: !!bookId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};
