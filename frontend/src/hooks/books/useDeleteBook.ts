import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import api from '@/lib/api';

export const useDeleteBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => api.delete(`/books/${id}`),

    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['books'] });

      toast.success('Book deleted successfully');
    },

    onError: (error: Error) => {
      toast.error(`Failed to delete book: ${error.message}`);
    },
  });
};
