import { z } from 'zod';
import { ReadingStatus } from '@/types/book';

export const bookSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  annotation: z.string().optional(),
  authors: z.string().optional(),
  description: z.string().optional(),
  isbn10: z.string().optional(),
  isbn13: z.string().optional(),
  pages: z.coerce.number().positive().optional().or(z.literal('')),
  publicationDate: z.string().optional(),
  publisher: z.string().optional(),
  tags: z.string().optional(),
  imageFile: z.instanceof(File).optional(),
  status: z.nativeEnum(ReadingStatus).optional(),
  startedReadingDate: z.string().optional(),
  finishedReadingDate: z.string().optional(),
});

export type BookFormSchema = z.infer<typeof bookSchema>;
