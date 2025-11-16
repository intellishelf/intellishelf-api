import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus } from 'lucide-react';
import { Layout } from '../components/layout/Layout';
import { Button } from '../components/ui';
import { BooksGrid } from '../components/books/BooksGrid';
import { EmptyState } from '../components/books/EmptyState';
import { LoadingSkeleton } from '../components/books/LoadingSkeleton';
import { Pagination } from '../components/common/Pagination';
import { AddBookModal } from '../components/books/AddBookModal';
import { EditBookModal } from '../components/books/EditBookModal';
import { DeleteBookModal } from '../components/books/DeleteBookModal';
import { useBooks } from '../hooks/books/useBooks';
import type { Book, BookOrderBy } from '../types/book';

export const DashboardPage = () => {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [orderBy] = useState<BookOrderBy>(3); // Default to Added
  const [ascending] = useState(false);

  // Modal states
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editingBook, setEditingBook] = useState<Book | null>(null);
  const [deletingBook, setDeletingBook] = useState<{
    id: string;
    title: string;
  } | null>(null);

  // Fetch books
  const { data, isLoading, isError } = useBooks({
    page,
    pageSize,
    orderBy,
    ascending,
  });

  const handleEditBook = (book: Book) => {
    setEditingBook(book);
  };

  const handleDeleteBook = (bookId: string) => {
    const book = data?.items.find((b) => b.id === bookId);
    if (book) {
      setDeletingBook({ id: book.id, title: book.title });
    }
  };

  const handleClickBook = (book: Book) => {
    navigate(`/books/${book.id}`);
  };

  const handlePageChange = (newPage: number) => {
    setPage(newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handlePageSizeChange = (newPageSize: number) => {
    setPageSize(newPageSize);
    setPage(1);
  };

  return (
    <Layout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">My Books</h1>
            <p className="mt-1 text-sm text-gray-500">
              {data?.totalCount || 0} books in your library
            </p>
          </div>
          <Button onClick={() => setIsAddModalOpen(true)}>
            <Plus className="h-5 w-5 mr-2" />
            Add Book
          </Button>
        </div>

        {/* Content */}
        {isLoading ? (
          <LoadingSkeleton />
        ) : isError ? (
          <div className="text-center py-12">
            <p className="text-red-600">Failed to load books. Please try again.</p>
          </div>
        ) : data?.items.length === 0 ? (
          <EmptyState onAddBook={() => setIsAddModalOpen(true)} />
        ) : (
          <>
            <BooksGrid
              books={data?.items || []}
              onEditBook={handleEditBook}
              onDeleteBook={handleDeleteBook}
              onClickBook={handleClickBook}
            />

            {/* Pagination */}
            {data && data.totalPages > 1 && (
              <div className="mt-8">
                <Pagination
                  currentPage={page}
                  totalPages={data.totalPages}
                  onPageChange={handlePageChange}
                  pageSize={pageSize}
                  onPageSizeChange={handlePageSizeChange}
                  totalCount={data.totalCount}
                />
              </div>
            )}
          </>
        )}
      </div>

      {/* Modals */}
      <AddBookModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
      />

      {editingBook && (
        <EditBookModal
          isOpen={!!editingBook}
          onClose={() => setEditingBook(null)}
          book={editingBook}
        />
      )}

      {deletingBook && (
        <DeleteBookModal
          isOpen={!!deletingBook}
          onClose={() => setDeletingBook(null)}
          bookId={deletingBook.id}
          bookTitle={deletingBook.title}
        />
      )}
    </Layout>
  );
};
