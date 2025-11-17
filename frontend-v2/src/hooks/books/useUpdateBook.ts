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

export const useUpdateBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: BookFormData }) => {
      const formData = toFormData(data);
      // Backend expects PUT with FormData
      return fetch(`${import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'}/api/books/${id}`, {
        method: 'PUT',
        credentials: 'include',
        body: formData,
      }).then(async (res) => {
        if (!res.ok) {
          const error = await res.text();
          throw new Error(error || `HTTP ${res.status}: ${res.statusText}`);
        }
        // PUT returns 204 No Content on success
        return res.status === 204 ? null : res.json();
      });
    },

    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['books'] });
      queryClient.invalidateQueries({ queryKey: ['books', variables.id] });

      toast.success('Book updated successfully');
    },

    onError: (error: Error) => {
      toast.error(`Failed to update book: ${error.message}`);
    },
  });
};
