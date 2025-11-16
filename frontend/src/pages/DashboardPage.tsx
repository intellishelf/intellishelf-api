import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Plus, X } from 'lucide-react';
import { Layout } from '../components/layout/Layout';
import { Button } from '../components/ui';
import { BooksGrid } from '../components/books/BooksGrid';
import { EmptyState } from '../components/books/EmptyState';
import { LoadingSkeleton } from '../components/books/LoadingSkeleton';
import { Pagination } from '../components/common/Pagination';
import { AddBookModal } from '../components/books/AddBookModal';
import { EditBookModal } from '../components/books/EditBookModal';
import { DeleteBookModal } from '../components/books/DeleteBookModal';
import { SearchBar } from '../components/search/SearchBar';
import { StatusFilter } from '../components/search/StatusFilter';
import { SearchEmptyState } from '../components/search/SearchEmptyState';
import { useBooks } from '../hooks/books/useBooks';
import { useSearchBooks } from '../hooks/books/useSearchBooks';
import { useDebounce } from '../hooks/utils/useDebounce';
import type { Book, BookOrderBy, ReadingStatus } from '../types/book';

export const DashboardPage = () => {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  // Get initial values from URL params
  const initialSearch = searchParams.get('search') || '';
  const initialStatus = searchParams.get('status');
  const initialPage = parseInt(searchParams.get('page') || '1', 10);
  const initialPageSize = parseInt(searchParams.get('pageSize') || '25', 10);

  // State
  const [searchTerm, setSearchTerm] = useState(initialSearch);
  const [selectedStatus, setSelectedStatus] = useState<ReadingStatus | null>(
    initialStatus ? parseInt(initialStatus, 10) as ReadingStatus : null
  );
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [orderBy] = useState<BookOrderBy>(3); // Default to Added
  const [ascending] = useState(false);

  // Debounce search term
  const debouncedSearchTerm = useDebounce(searchTerm, 300);

  // Modal states
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editingBook, setEditingBook] = useState<Book | null>(null);
  const [deletingBook, setDeletingBook] = useState<{
    id: string;
    title: string;
  } | null>(null);

  // Determine if we're in search mode
  const isSearchMode = debouncedSearchTerm.trim().length > 0 || selectedStatus !== null;

  // Fetch books (regular or search)
  const regularBooks = useBooks(
    {
      page,
      pageSize,
      orderBy,
      ascending,
    },
    { enabled: !isSearchMode }
  );

  const searchResults = useSearchBooks(
    {
      searchTerm: debouncedSearchTerm,
      page,
      pageSize,
      status: selectedStatus ?? undefined,
    },
    { enabled: isSearchMode }
  );

  // Use the appropriate data source
  const { data, isLoading, isError } = isSearchMode ? searchResults : regularBooks;

  // Update URL params when search/filter changes
  useEffect(() => {
    const params: Record<string, string> = {};
    if (debouncedSearchTerm) params.search = debouncedSearchTerm;
    if (selectedStatus !== null) params.status = selectedStatus.toString();
    if (page !== 1) params.page = page.toString();
    if (pageSize !== 25) params.pageSize = pageSize.toString();

    setSearchParams(params, { replace: true });
  }, [debouncedSearchTerm, selectedStatus, page, pageSize, setSearchParams]);

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

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    setPage(1); // Reset to first page on search
  };

  const handleStatusChange = (status: ReadingStatus | null) => {
    setSelectedStatus(status);
    setPage(1); // Reset to first page on filter change
  };

  const handleClearFilters = () => {
    setSearchTerm('');
    setSelectedStatus(null);
    setPage(1);
  };

  const hasActiveFilters = searchTerm.trim().length > 0 || selectedStatus !== null;

  return (
    <Layout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-6">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">My Books</h1>
            <p className="mt-1 text-sm text-gray-500">
              {data?.totalCount || 0} books {isSearchMode ? 'found' : 'in your library'}
            </p>
          </div>
          <Button onClick={() => setIsAddModalOpen(true)} className="mt-4 sm:mt-0">
            <Plus className="h-5 w-5 mr-2" />
            Add Book
          </Button>
        </div>

        {/* Search and Filters */}
        <div className="mb-6 space-y-4">
          <SearchBar
            value={searchTerm}
            onChange={handleSearchChange}
            className="w-full"
          />

          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <StatusFilter
              selectedStatus={selectedStatus}
              onChange={handleStatusChange}
            />

            {hasActiveFilters && (
              <Button
                variant="outline"
                onClick={handleClearFilters}
                className="sm:ml-auto"
              >
                <X className="h-4 w-4 mr-2" />
                Clear Filters
              </Button>
            )}
          </div>
        </div>

        {/* Content */}
        {isLoading ? (
          <LoadingSkeleton />
        ) : isError ? (
          <div className="text-center py-12">
            <p className="text-red-600">Failed to load books. Please try again.</p>
          </div>
        ) : data?.items.length === 0 ? (
          isSearchMode ? (
            <SearchEmptyState
              searchTerm={debouncedSearchTerm}
              onClear={handleClearFilters}
            />
          ) : (
            <EmptyState onAddBook={() => setIsAddModalOpen(true)} />
          )
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
