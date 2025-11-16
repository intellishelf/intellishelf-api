import { Modal } from '../ui';
import { BookForm } from './BookForm';
import { useUpdateBook } from '../../hooks/books/useUpdateBook';
import type { Book, AddBookRequest } from '../../types/book';

interface EditBookModalProps {
  isOpen: boolean;
  onClose: () => void;
  book: Book;
}

export const EditBookModal = ({ isOpen, onClose, book }: EditBookModalProps) => {
  const { mutate: updateBook, isPending } = useUpdateBook();

  const handleSubmit = (data: AddBookRequest) => {
    updateBook(
      { bookId: book.id, data },
      {
        onSuccess: () => {
          onClose();
        },
      }
    );
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Edit Book" size="xl">
      <BookForm
        book={book}
        onSubmit={handleSubmit}
        onCancel={onClose}
        isLoading={isPending}
      />
    </Modal>
  );
};
