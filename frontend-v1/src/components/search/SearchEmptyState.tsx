import { SearchX } from 'lucide-react';
import { Button } from '../ui/Button';

interface SearchEmptyStateProps {
  searchTerm?: string;
  onClear: () => void;
}

export const SearchEmptyState: React.FC<SearchEmptyStateProps> = ({
  searchTerm,
  onClear,
}) => {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-4">
      <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mb-4">
        <SearchX className="w-8 h-8 text-gray-400" />
      </div>
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        No books found
      </h3>
      {searchTerm ? (
        <>
          <p className="text-gray-600 text-center mb-6 max-w-md">
            No results for "<span className="font-medium">{searchTerm}</span>".
            Try a different search term or clear filters.
          </p>
          <div className="flex flex-col sm:flex-row gap-3">
            <Button variant="outline" onClick={onClear}>
              Clear search
            </Button>
          </div>
        </>
      ) : (
        <p className="text-gray-600 text-center mb-6 max-w-md">
          Try adjusting your filters or search criteria.
        </p>
      )}
      <div className="mt-6 text-sm text-gray-500">
        <p className="font-medium mb-2">Search tips:</p>
        <ul className="list-disc list-inside space-y-1 text-left">
          <li>Try different keywords</li>
          <li>Check for typos</li>
          <li>Use fewer or different filters</li>
          <li>Search by author, title, or ISBN</li>
        </ul>
      </div>
    </div>
  );
};
