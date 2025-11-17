import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import type { Book } from '@/types/book';

export const useBook = (id: string) => {
  return useQuery({
    queryKey: ['books', id],
    queryFn: () => api.get<Book>(`/books/${id}`),
    enabled: !!id,
  });
};
