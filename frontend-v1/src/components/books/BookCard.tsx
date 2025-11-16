import { BookOpen, Edit, Trash2 } from 'lucide-react';
import type { Book } from '../../types/book';
import { StatusBadge } from './StatusBadge';
import { Button } from '../ui';

interface BookCardProps {
  book: Book;
  onEdit: (book: Book) => void;
  onDelete: (bookId: string) => void;
  onClick?: (book: Book) => void;
}

export const BookCard = ({ book, onEdit, onDelete, onClick }: BookCardProps) => {
  return (
    <div className="group relative bg-white rounded-lg shadow-sm hover:shadow-md transition-shadow overflow-hidden cursor-pointer">
      {/* Cover Image */}
      <div
        className="aspect-[2/3] bg-gray-100"
        onClick={() => onClick?.(book)}
      >
        {book.coverImageUrl ? (
          <img
            src={book.coverImageUrl}
            alt={book.title}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-400">
            <BookOpen size={48} />
          </div>
        )}
      </div>

      {/* Overlay on hover */}
      <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-50 transition-opacity flex items-center justify-center gap-2 opacity-0 group-hover:opacity-100">
        <Button
          variant="secondary"
          size="sm"
          onClick={(e) => {
            e.stopPropagation();
            onEdit(book);
          }}
        >
          <Edit size={16} />
        </Button>
        <Button
          variant="danger"
          size="sm"
          onClick={(e) => {
            e.stopPropagation();
            onDelete(book.id);
          }}
        >
          <Trash2 size={16} />
        </Button>
      </div>

      {/* Info */}
      <div className="p-4" onClick={() => onClick?.(book)}>
        <h3 className="font-semibold text-gray-900 truncate" title={book.title}>
          {book.title}
        </h3>
        {book.authors && (
          <p className="text-sm text-gray-600 truncate" title={book.authors}>
            {book.authors}
          </p>
        )}
        <div className="mt-2">
          <StatusBadge status={book.status} />
        </div>
      </div>
    </div>
  );
};
