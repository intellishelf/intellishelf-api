import { useState } from "react";
import SearchBar from "@/components/SearchBar";
import BookCard from "@/components/BookCard";
import BooksGridSkeleton from "@/components/books/BooksGridSkeleton";
import EmptyState from "@/components/books/EmptyState";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useBooks } from "@/hooks/books/useBooks";
import { useSearchBooks } from "@/hooks/books/useSearchBooks";
import { useDebounce } from "@/hooks/utils/useDebounce";
import { ReadingStatus, BookOrderBy } from "@/types/book";

const Library = () => {
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedStatus, setSelectedStatus] = useState<ReadingStatus | null>(null);
  const [orderBy, setOrderBy] = useState<BookOrderBy>(BookOrderBy.Added);
  const [ascending, setAscending] = useState(false);

  // Debounce search term
  const debouncedSearch = useDebounce(searchQuery, 300);

  // Use search or normal query based on whether there's a search term or filter
  const hasSearchOrFilter = debouncedSearch.length > 0 || selectedStatus !== null;

  const { data: searchData, isLoading: isSearching } = useSearchBooks({
    searchTerm: debouncedSearch,
    status: selectedStatus,
    page: 1,
    pageSize: 50,
  });

  const { data: allBooks, isLoading: isLoadingAll } = useBooks({
    page: 1,
    pageSize: 50,
    orderBy,
    ascending,
  });

  const books = hasSearchOrFilter ? searchData?.items : allBooks?.items;
  const totalCount = hasSearchOrFilter ? searchData?.totalCount : allBooks?.totalCount;
  const isLoading = hasSearchOrFilter ? isSearching : isLoadingAll;

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <header className="border-b border-border p-6">
        <div className="flex items-center justify-between gap-4 mb-6">
          <div className="flex-1 max-w-md">
            <SearchBar value={searchQuery} onChange={setSearchQuery} />
          </div>
          <Tabs
            value={selectedStatus?.toString() ?? 'all'}
            onValueChange={(v) => setSelectedStatus(v === 'all' ? null : Number(v) as ReadingStatus)}
          >
            <TabsList>
              <TabsTrigger value="all">All</TabsTrigger>
              <TabsTrigger value="0">Unread</TabsTrigger>
              <TabsTrigger value="1">Reading</TabsTrigger>
              <TabsTrigger value="2">Read</TabsTrigger>
            </TabsList>
          </Tabs>
        </div>

        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-foreground mb-1">Library</h1>
            <p className="text-muted-foreground">
              {totalCount ?? 0} {totalCount === 1 ? "book" : "books"}
            </p>
          </div>
        </div>
      </header>

      {/* Books Grid */}
      <div className="flex-1 overflow-auto p-6">
        {isLoading ? (
          <BooksGridSkeleton />
        ) : books && books.length > 0 ? (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-6">
            {books.map((book) => (
              <BookCard key={book.id} book={book} />
            ))}
          </div>
        ) : hasSearchOrFilter ? (
          <EmptyState
            title="No books found"
            description="Try adjusting your search or filters"
            showAddButton={false}
          />
        ) : (
          <EmptyState />
        )}
      </div>
    </div>
  );
};

export default Library;
