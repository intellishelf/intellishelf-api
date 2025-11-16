import { Modal } from '../ui';
import { BookForm } from './BookForm';
import { useAddBook } from '../../hooks/books/useAddBook';
import { useUpdateBook } from '../../hooks/books/useUpdateBook';
import type { Book, AddBookRequest } from '../../types/book';

interface BookFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  book?: Book; // If provided, we're editing; otherwise, we're adding
}

/**
 * Generic modal for adding or editing a book
 * Automatically detects mode based on presence of book prop
 */
export const BookFormModal = ({ isOpen, onClose, book }: BookFormModalProps) => {
  const { mutate: addBook, isPending: isAdding } = useAddBook();
  const { mutate: updateBook, isPending: isUpdating } = useUpdateBook();

  const isEditMode = !!book;
  const isPending = isEditMode ? isUpdating : isAdding;
  const title = isEditMode ? 'Edit Book' : 'Add New Book';

  const handleSubmit = (data: AddBookRequest) => {
    if (isEditMode) {
      updateBook(
        { bookId: book.id, data },
        {
          onSuccess: () => {
            onClose();
          },
        }
      );
    } else {
      addBook(data, {
        onSuccess: () => {
          onClose();
        },
      });
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title} size="xl">
      <BookForm
        book={book}
        onSubmit={handleSubmit}
        onCancel={onClose}
        isLoading={isPending}
      />
    </Modal>
  );
};
