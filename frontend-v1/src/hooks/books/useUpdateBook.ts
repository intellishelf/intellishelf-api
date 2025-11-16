import { useMutation, useQueryClient } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';
import type { AddBookRequest } from '../../types/book';
import toast from 'react-hot-toast';
import { extractErrorMessage } from '../../utils/errors';

export const useUpdateBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ bookId, data }: { bookId: string; data: AddBookRequest }) =>
      booksApi.updateBook(bookId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['books'] });
      toast.success('Book updated successfully!');
    },
    onError: (error) => {
      const message = extractErrorMessage(error, 'Failed to update book');
      toast.error(message);
    },
  });
};
