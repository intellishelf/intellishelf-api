import type { AddBookRequest } from '../types/book';

/**
 * Builds a FormData object from book request data
 * Handles all book fields including optional ones and file uploads
 */
export const buildBookFormData = (data: AddBookRequest): FormData => {
  const formData = new FormData();

  // Required field
  formData.append('title', data.title);

  // Optional text fields
  if (data.annotation) formData.append('annotation', data.annotation);
  if (data.authors) formData.append('authors', data.authors);
  if (data.description) formData.append('description', data.description);
  if (data.isbn10) formData.append('isbn10', data.isbn10);
  if (data.isbn13) formData.append('isbn13', data.isbn13);
  if (data.publisher) formData.append('publisher', data.publisher);

  // Numeric fields
  if (data.pages !== undefined) formData.append('pages', data.pages.toString());
  if (data.status !== undefined) formData.append('status', data.status.toString());

  // Date fields
  if (data.publicationDate) formData.append('publicationDate', data.publicationDate);
  if (data.startedReadingDate) formData.append('startedReadingDate', data.startedReadingDate);
  if (data.finishedReadingDate) formData.append('finishedReadingDate', data.finishedReadingDate);

  // Array fields (tags)
  if (data.tags) {
    data.tags.forEach((tag) => formData.append('tags', tag));
  }

  // File upload
  if (data.imageFile) formData.append('imageFile', data.imageFile);

  return formData;
};
