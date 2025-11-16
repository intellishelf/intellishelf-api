import { Modal } from '../ui';
import { BookForm } from './BookForm';
import { useAddBook } from '../../hooks/books/useAddBook';
import type { AddBookRequest } from '../../types/book';

interface AddBookModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const AddBookModal = ({ isOpen, onClose }: AddBookModalProps) => {
  const { mutate: addBook, isPending } = useAddBook();

  const handleSubmit = (data: AddBookRequest) => {
    addBook(data, {
      onSuccess: () => {
        onClose();
      },
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add New Book" size="xl">
      <BookForm onSubmit={handleSubmit} onCancel={onClose} isLoading={isPending} />
    </Modal>
  );
};
