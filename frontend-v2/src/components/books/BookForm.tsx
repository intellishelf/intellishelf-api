import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import ImageUpload from './ImageUpload';
import { bookSchema, type BookFormSchema } from '@/lib/schemas/bookSchema';
import { useAddBook } from '@/hooks/books/useAddBook';
import { useUpdateBook } from '@/hooks/books/useUpdateBook';
import type { Book, ReadingStatus } from '@/types/book';

interface BookFormProps {
  book?: Book;
  onSuccess?: () => void;
}

const BookForm = ({ book, onSuccess }: BookFormProps) => {
  const { mutate: addBook, isPending: isAdding } = useAddBook();
  const { mutate: updateBook, isPending: isUpdating } = useUpdateBook();
  const isPending = isAdding || isUpdating;

  const form = useForm<BookFormSchema>({
    resolver: zodResolver(bookSchema),
    defaultValues: book
      ? {
          title: book.title,
          annotation: book.annotation || '',
          authors: book.authors || '',
          description: book.description || '',
          isbn10: book.isbn10 || '',
          isbn13: book.isbn13 || '',
          pages: book.pages || undefined,
          publicationDate: book.publicationDate
            ? book.publicationDate.split('T')[0]
            : '',
          publisher: book.publisher || '',
          tags: book.tags?.join(', ') || '',
          status: book.status,
          startedReadingDate: book.startedReadingDate
            ? book.startedReadingDate.split('T')[0]
            : '',
          finishedReadingDate: book.finishedReadingDate
            ? book.finishedReadingDate.split('T')[0]
            : '',
        }
      : {
          status: 0, // Default to Unread
        },
  });

  const onSubmit = (data: BookFormSchema) => {
    const formData = {
      ...data,
      pages: data.pages ? Number(data.pages) : undefined,
    };

    if (book) {
      updateBook(
        { id: book.id, data: formData },
        {
          onSuccess: () => {
            onSuccess?.();
          },
        }
      );
    } else {
      addBook(formData, {
        onSuccess: () => {
          form.reset();
          onSuccess?.();
        },
      });
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        <FormField
          control={form.control}
          name="imageFile"
          render={({ field: { value, onChange, ...field } }) => (
            <FormItem>
              <FormLabel>Cover Image</FormLabel>
              <FormControl>
                <ImageUpload
                  value={value || (book?.coverImageUrl ?? null)}
                  onChange={onChange}
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="title"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Title *</FormLabel>
              <FormControl>
                <Input placeholder="The Great Gatsby" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="authors"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Author(s)</FormLabel>
              <FormControl>
                <Input
                  placeholder="F. Scott Fitzgerald, ..."
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Description</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="A brief description..."
                  className="resize-none"
                  rows={4}
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="isbn10"
            render={({ field }) => (
              <FormItem>
                <FormLabel>ISBN-10</FormLabel>
                <FormControl>
                  <Input placeholder="0-123-45678-9" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="isbn13"
            render={({ field }) => (
              <FormItem>
                <FormLabel>ISBN-13</FormLabel>
                <FormControl>
                  <Input placeholder="978-0-123-45678-9" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="pages"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Pages</FormLabel>
                <FormControl>
                  <Input type="number" placeholder="320" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="publicationDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Published Date</FormLabel>
                <FormControl>
                  <Input type="date" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="publisher"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Publisher</FormLabel>
              <FormControl>
                <Input placeholder="Scribner" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="annotation"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Personal Notes</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Your personal notes about this book..."
                  className="resize-none"
                  rows={3}
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="tags"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Tags</FormLabel>
              <FormControl>
                <Input
                  placeholder="fiction, classic, american (comma separated)"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="status"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Reading Status</FormLabel>
              <Select
                onValueChange={(value) => field.onChange(Number(value))}
                defaultValue={field.value?.toString()}
              >
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select reading status" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="0">Unread</SelectItem>
                  <SelectItem value="1">Reading</SelectItem>
                  <SelectItem value="2">Read</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {form.watch('status') === 1 && (
          <FormField
            control={form.control}
            name="startedReadingDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Started Reading</FormLabel>
                <FormControl>
                  <Input type="date" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        )}

        {form.watch('status') === 2 && (
          <>
            <FormField
              control={form.control}
              name="startedReadingDate"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Started Reading</FormLabel>
                  <FormControl>
                    <Input type="date" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="finishedReadingDate"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Finished Reading</FormLabel>
                  <FormControl>
                    <Input type="date" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </>
        )}

        <Button type="submit" disabled={isPending} className="w-full">
          {isPending
            ? book
              ? 'Updating...'
              : 'Adding...'
            : book
            ? 'Update Book'
            : 'Add Book'}
        </Button>
      </form>
    </Form>
  );
};

export default BookForm;
