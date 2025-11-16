import { BookOpen, Search } from 'lucide-react';
import { Button } from '../ui';

interface EmptyStateProps {
  variant?: 'no-books' | 'no-results';
  onAddBook?: () => void;
}

export const EmptyState = ({ variant = 'no-books', onAddBook }: EmptyStateProps) => {
  if (variant === 'no-results') {
    return (
      <div className="text-center py-12">
        <Search className="mx-auto h-12 w-12 text-gray-400" />
        <h3 className="mt-2 text-sm font-medium text-gray-900">No books found</h3>
        <p className="mt-1 text-sm text-gray-500">
          Try adjusting your search or filters
        </p>
      </div>
    );
  }

  return (
    <div className="text-center py-12">
      <BookOpen className="mx-auto h-12 w-12 text-gray-400" />
      <h3 className="mt-2 text-sm font-medium text-gray-900">No books yet</h3>
      <p className="mt-1 text-sm text-gray-500">
        Get started by adding your first book
      </p>
      {onAddBook && (
        <div className="mt-6">
          <Button onClick={onAddBook}>Add Book</Button>
        </div>
      )}
    </div>
  );
};
