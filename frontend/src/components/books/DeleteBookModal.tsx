import { AlertTriangle } from 'lucide-react';
import { Modal, Button } from '../ui';
import { useDeleteBook } from '../../hooks/books/useDeleteBook';

interface DeleteBookModalProps {
  isOpen: boolean;
  onClose: () => void;
  bookId: string;
  bookTitle: string;
}

export const DeleteBookModal = ({
  isOpen,
  onClose,
  bookId,
  bookTitle,
}: DeleteBookModalProps) => {
  const { mutate: deleteBook, isPending } = useDeleteBook();

  const handleDelete = () => {
    deleteBook(bookId, {
      onSuccess: () => {
        onClose();
      },
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Delete Book" size="md">
      <div className="space-y-4">
        <div className="flex items-start gap-4">
          <div className="flex-shrink-0">
            <AlertTriangle className="h-6 w-6 text-red-600" />
          </div>
          <div>
            <p className="text-sm text-gray-700">
              Are you sure you want to delete{' '}
              <span className="font-semibold">{bookTitle}</span>?
            </p>
            <p className="mt-2 text-sm text-gray-500">
              This action cannot be undone. All book data, including the cover image,
              will be permanently removed.
            </p>
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-4 border-t">
          <Button variant="outline" onClick={onClose} disabled={isPending}>
            Cancel
          </Button>
          <Button variant="danger" onClick={handleDelete} isLoading={isPending}>
            Delete Book
          </Button>
        </div>
      </div>
    </Modal>
  );
};
