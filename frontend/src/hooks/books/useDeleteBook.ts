import { useMutation, useQueryClient } from '@tanstack/react-query';
import { booksApi } from '../../api/endpoints/books';
import type { PagedResult, Book } from '../../types/book';
import toast from 'react-hot-toast';
import { extractErrorMessage } from '../../utils/errors';

export const useDeleteBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (bookId: string) => booksApi.deleteBook(bookId),
    onMutate: async (bookId) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['books'] });

      // Snapshot previous value
      const previousBooks = queryClient.getQueryData(['books']);

      // Optimistically update to remove book
      queryClient.setQueriesData({ queryKey: ['books'] }, (old: PagedResult<Book> | undefined) => {
        if (!old) return old;
        return {
          ...old,
          items: old.items.filter((book) => book.id !== bookId),
          totalCount: old.totalCount - 1,
        };
      });

      return { previousBooks };
    },
    onError: (error, _bookId, context) => {
      // Rollback on error
      if (context?.previousBooks) {
        queryClient.setQueryData(['books'], context.previousBooks);
      }
      const message = extractErrorMessage(error, 'Failed to delete book');
      toast.error(message);
    },
    onSuccess: () => {
      toast.success('Book deleted successfully!');
    },
    onSettled: () => {
      // Refetch after success or error
      queryClient.invalidateQueries({ queryKey: ['books'] });
    },
  });
};
