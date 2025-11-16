import type { Book } from '../../types/book';
import { BookCard } from './BookCard';

interface BooksGridProps {
  books: Book[];
  onEditBook: (book: Book) => void;
  onDeleteBook: (bookId: string) => void;
  onClickBook?: (book: Book) => void;
}

export const BooksGrid = ({
  books,
  onEditBook,
  onDeleteBook,
  onClickBook,
}: BooksGridProps) => {
  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-6">
      {books.map((book) => (
        <BookCard
          key={book.id}
          book={book}
          onEdit={onEditBook}
          onDelete={onDeleteBook}
          onClick={onClickBook}
        />
      ))}
    </div>
  );
};
