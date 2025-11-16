import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { Book } from '../../types/book';
import { ReadingStatus } from '../../types/book';
import { Input, Button } from '../ui';
import { ImageUpload } from './ImageUpload';

const bookSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  authors: z.string().optional(),
  description: z.string().optional(),
  annotation: z.string().optional(),
  isbn10: z.string().optional(),
  isbn13: z.string().optional(),
  pages: z.number().optional(),
  publicationDate: z.string().optional(),
  publisher: z.string().optional(),
  tags: z.array(z.string()).optional(),
  status: z.nativeEnum(ReadingStatus).optional(),
  startedReadingDate: z.string().optional(),
  finishedReadingDate: z.string().optional(),
  imageFile: z.instanceof(File).optional().nullable(),
});

type BookFormData = z.infer<typeof bookSchema>;

interface BookFormProps {
  book?: Book;
  onSubmit: (data: BookFormData) => void;
  onCancel: () => void;
  isLoading?: boolean;
}

export const BookForm = ({ book, onSubmit, onCancel, isLoading }: BookFormProps) => {
  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
    watch,
  } = useForm<BookFormData>({
    resolver: zodResolver(bookSchema),
    defaultValues: {
      title: book?.title || '',
      authors: book?.authors || '',
      description: book?.description || '',
      annotation: book?.annotation || '',
      isbn10: book?.isbn10 || '',
      isbn13: book?.isbn13 || '',
      pages: book?.pages || undefined,
      publicationDate: book?.publicationDate || '',
      publisher: book?.publisher || '',
      status: book?.status ?? ReadingStatus.Unread,
      startedReadingDate: book?.startedReadingDate || '',
      finishedReadingDate: book?.finishedReadingDate || '',
      imageFile: null,
    },
  });

  const imageFile = watch('imageFile');

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Image Upload */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Book Cover
        </label>
        <ImageUpload
          value={imageFile || book?.coverImageUrl}
          onChange={(file) => setValue('imageFile', file)}
          error={errors.imageFile?.message}
        />
      </div>

      {/* Title */}
      <div>
        <Input
          label="Title *"
          {...register('title')}
          error={errors.title?.message}
          placeholder="The Great Gatsby"
        />
      </div>

      {/* Authors */}
      <div>
        <Input
          label="Author(s)"
          {...register('authors')}
          error={errors.authors?.message}
          placeholder="F. Scott Fitzgerald"
        />
      </div>

      {/* Description */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Description
        </label>
        <textarea
          {...register('description')}
          rows={4}
          className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
          placeholder="Brief description of the book..."
        />
        {errors.description && (
          <p className="mt-1 text-sm text-red-600">{errors.description.message}</p>
        )}
      </div>

      {/* Two column layout for smaller fields */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* ISBN-10 */}
        <div>
          <Input
            label="ISBN-10"
            {...register('isbn10')}
            error={errors.isbn10?.message}
            placeholder="0-123456-78-9"
          />
        </div>

        {/* ISBN-13 */}
        <div>
          <Input
            label="ISBN-13"
            {...register('isbn13')}
            error={errors.isbn13?.message}
            placeholder="978-0-123456-78-9"
          />
        </div>

        {/* Publisher */}
        <div>
          <Input
            label="Publisher"
            {...register('publisher')}
            error={errors.publisher?.message}
            placeholder="Scribner"
          />
        </div>

        {/* Publication Date */}
        <div>
          <Input
            label="Publication Date"
            type="date"
            {...register('publicationDate')}
            error={errors.publicationDate?.message}
          />
        </div>

        {/* Pages */}
        <div>
          <Input
            label="Pages"
            type="number"
            {...register('pages', { valueAsNumber: true })}
            error={errors.pages?.message}
            placeholder="218"
          />
        </div>

        {/* Reading Status */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Reading Status
          </label>
          <select
            {...register('status', { valueAsNumber: true })}
            className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
          >
            <option value={ReadingStatus.Unread}>Unread</option>
            <option value={ReadingStatus.Reading}>Reading</option>
            <option value={ReadingStatus.Read}>Read</option>
          </select>
        </div>

        {/* Started Reading Date */}
        <div>
          <Input
            label="Started Reading"
            type="date"
            {...register('startedReadingDate')}
            error={errors.startedReadingDate?.message}
          />
        </div>

        {/* Finished Reading Date */}
        <div>
          <Input
            label="Finished Reading"
            type="date"
            {...register('finishedReadingDate')}
            error={errors.finishedReadingDate?.message}
          />
        </div>
      </div>

      {/* Annotation */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Personal Notes
        </label>
        <textarea
          {...register('annotation')}
          rows={3}
          className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
          placeholder="Your thoughts about this book..."
        />
        {errors.annotation && (
          <p className="mt-1 text-sm text-red-600">{errors.annotation.message}</p>
        )}
      </div>

      {/* Form Actions */}
      <div className="flex justify-end gap-3 pt-4 border-t">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>
          Cancel
        </Button>
        <Button type="submit" isLoading={isLoading}>
          {book ? 'Update Book' : 'Add Book'}
        </Button>
      </div>
    </form>
  );
};
