import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Edit, Trash2, BookOpen } from 'lucide-react';
import { Layout } from '../components/layout/Layout';
import { Button, Spinner } from '../components/ui';
import { StatusBadge } from '../components/books/StatusBadge';
import { BookFormModal } from '../components/books/BookFormModal';
import { DeleteBookModal } from '../components/books/DeleteBookModal';
import { useBook } from '../hooks/books/useBook';

export const BookDetailPage = () => {
  const { bookId } = useParams<{ bookId: string }>();
  const navigate = useNavigate();
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  const { data: book, isLoading, isError } = useBook(bookId!);

  if (isLoading) {
    return (
      <Layout>
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="flex justify-center items-center h-64">
            <Spinner size="lg" />
          </div>
        </div>
      </Layout>
    );
  }

  if (isError || !book) {
    return (
      <Layout>
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center py-12">
            <p className="text-red-600">Failed to load book details.</p>
            <Button onClick={() => navigate('/dashboard')} className="mt-4">
              Go Back
            </Button>
          </div>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Back Button */}
        <Button
          variant="ghost"
          onClick={() => navigate('/dashboard')}
          className="mb-6"
        >
          <ArrowLeft className="h-5 w-5 mr-2" />
          Back to Books
        </Button>

        {/* Book Content */}
        <div className="bg-white rounded-lg shadow-sm overflow-hidden">
          <div className="md:flex">
            {/* Book Cover */}
            <div className="md:flex-shrink-0 md:w-1/3 bg-gray-100">
              {book.coverImageUrl ? (
                <img
                  src={book.coverImageUrl}
                  alt={book.title}
                  className="w-full h-full object-cover"
                />
              ) : (
                <div className="w-full h-96 flex items-center justify-center text-gray-400">
                  <BookOpen size={96} />
                </div>
              )}
            </div>

            {/* Book Details */}
            <div className="p-8 md:w-2/3">
              <div className="flex items-start justify-between mb-4">
                <div className="flex-1">
                  <h1 className="text-3xl font-bold text-gray-900 mb-2">
                    {book.title}
                  </h1>
                  {book.authors && (
                    <p className="text-xl text-gray-600 mb-4">{book.authors}</p>
                  )}
                  <StatusBadge status={book.status} />
                </div>
                <div className="flex gap-2 ml-4">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setIsEditModalOpen(true)}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="danger"
                    size="sm"
                    onClick={() => setIsDeleteModalOpen(true)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>

              {/* Description */}
              {book.description && (
                <div className="mb-6">
                  <h2 className="text-lg font-semibold text-gray-900 mb-2">
                    Description
                  </h2>
                  <p className="text-gray-700 whitespace-pre-wrap">
                    {book.description}
                  </p>
                </div>
              )}

              {/* Book Details Grid */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
                {book.publisher && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Publisher</h3>
                    <p className="text-gray-900">{book.publisher}</p>
                  </div>
                )}

                {book.publicationDate && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">
                      Publication Date
                    </h3>
                    <p className="text-gray-900">
                      {new Date(book.publicationDate).toLocaleDateString()}
                    </p>
                  </div>
                )}

                {book.isbn10 && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">ISBN-10</h3>
                    <p className="text-gray-900">{book.isbn10}</p>
                  </div>
                )}

                {book.isbn13 && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">ISBN-13</h3>
                    <p className="text-gray-900">{book.isbn13}</p>
                  </div>
                )}

                {book.pages !== undefined && book.pages > 0 && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Pages</h3>
                    <p className="text-gray-900">{book.pages}</p>
                  </div>
                )}

                {book.startedReadingDate && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">
                      Started Reading
                    </h3>
                    <p className="text-gray-900">
                      {new Date(book.startedReadingDate).toLocaleDateString()}
                    </p>
                  </div>
                )}

                {book.finishedReadingDate && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">
                      Finished Reading
                    </h3>
                    <p className="text-gray-900">
                      {new Date(book.finishedReadingDate).toLocaleDateString()}
                    </p>
                  </div>
                )}
              </div>

              {/* Tags */}
              {book.tags && book.tags.length > 0 && (
                <div className="mb-6">
                  <h3 className="text-sm font-medium text-gray-500 mb-2">Tags</h3>
                  <div className="flex flex-wrap gap-2">
                    {book.tags.map((tag, index) => (
                      <span
                        key={index}
                        className="inline-flex items-center px-3 py-1 rounded-full text-sm bg-gray-100 text-gray-700"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {/* Personal Notes */}
              {book.annotation && (
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 mb-2">
                    Personal Notes
                  </h2>
                  <p className="text-gray-700 whitespace-pre-wrap bg-gray-50 p-4 rounded-lg">
                    {book.annotation}
                  </p>
                </div>
              )}

              {/* Added Date */}
              <div className="mt-6 pt-6 border-t border-gray-200">
                <p className="text-sm text-gray-500">
                  Added on {new Date(book.createdDate).toLocaleDateString()}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Modals */}
      {isEditModalOpen && (
        <BookFormModal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          book={book}
        />
      )}

      {isDeleteModalOpen && (
        <DeleteBookModal
          isOpen={isDeleteModalOpen}
          onClose={() => setIsDeleteModalOpen(false)}
          bookId={book.id}
          bookTitle={book.title}
        />
      )}
    </Layout>
  );
};
