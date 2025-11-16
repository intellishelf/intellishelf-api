import { useMutation, useQueryClient } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';
import type { AddBookRequest } from '../../types/book';
import toast from 'react-hot-toast';

export const useAddBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: AddBookRequest) => booksApi.addBook(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['books'] });
      toast.success('Book added successfully!');
    },
    onError: (error: any) => {
      const message = error.response?.data?.detail || 'Failed to add book';
      toast.error(message);
    },
  });
};
