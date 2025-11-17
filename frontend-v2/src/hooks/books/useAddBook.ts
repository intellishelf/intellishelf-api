import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import api from '@/lib/api';
import type { Book, BookFormData } from '@/types/book';

// Helper to convert BookFormData to FormData for backend
const toFormData = (data: BookFormData): FormData => {
  const formData = new FormData();

  // Add required field
  formData.append('title', data.title);

  // Add optional fields if they exist
  if (data.annotation) formData.append('annotation', data.annotation);
  if (data.description) formData.append('description', data.description);
  if (data.isbn10) formData.append('isbn10', data.isbn10);
  if (data.isbn13) formData.append('isbn13', data.isbn13);
  if (data.pages) formData.append('pages', data.pages.toString());
  if (data.publicationDate) formData.append('publicationDate', data.publicationDate);
  if (data.publisher) formData.append('publisher', data.publisher);

  // Convert status enum to number
  if (data.status !== undefined) formData.append('status', data.status.toString());

  if (data.startedReadingDate) formData.append('startedReadingDate', data.startedReadingDate);
  if (data.finishedReadingDate) formData.append('finishedReadingDate', data.finishedReadingDate);

  // Split comma-separated strings into arrays
  if (data.authors) {
    const authorsArray = data.authors.split(',').map(a => a.trim()).filter(Boolean);
    authorsArray.forEach(author => formData.append('authors', author));
  }

  if (data.tags) {
    const tagsArray = data.tags.split(',').map(t => t.trim()).filter(Boolean);
    tagsArray.forEach(tag => formData.append('tags', tag));
  }

  // Add image file if present
  if (data.imageFile) {
    formData.append('imageFile', data.imageFile);
  }

  return formData;
};

export const useAddBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BookFormData) => {
      const formData = toFormData(data);
      return api.upload<Book>('/books', formData);
    },

    onMutate: async () => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['books'] });

      // Snapshot previous value
      const previousBooks = queryClient.getQueryData(['books']);

      return { previousBooks };
    },

    onSuccess: () => {
      // Invalidate to refetch with server data
      queryClient.invalidateQueries({ queryKey: ['books'] });

      toast.success('Book added successfully');
    },

    onError: (error: Error, _variables, context) => {
      // Rollback on error
      if (context?.previousBooks) {
        queryClient.setQueryData(['books'], context.previousBooks);
      }

      toast.error(`Failed to add book: ${error.message}`);
    },
  });
};
