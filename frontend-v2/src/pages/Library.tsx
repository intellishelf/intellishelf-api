import { useState, useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { X } from "lucide-react";
import SearchBar from "@/components/SearchBar";
import BookCard from "@/components/BookCard";
import BooksGridSkeleton from "@/components/books/BooksGridSkeleton";
import EmptyState from "@/components/books/EmptyState";
import SearchEmptyState from "@/components/SearchEmptyState";
import BooksPagination from "@/components/books/BooksPagination";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useBooks } from "@/hooks/books/useBooks";
import { useSearchBooks } from "@/hooks/books/useSearchBooks";
import { useDebounce } from "@/hooks/utils/useDebounce";
import { ReadingStatus, BookOrderBy } from "@/types/book";

const Library = () => {
  const [searchParams, setSearchParams] = useSearchParams();

  // Get initial values from URL params
  const initialSearch = searchParams.get("search") || "";
  const initialStatus = searchParams.get("status");
  const initialPage = parseInt(searchParams.get("page") || "1", 10);
  const initialPageSize = parseInt(searchParams.get("pageSize") || "24", 10);

  // State
  const [searchQuery, setSearchQuery] = useState(initialSearch);
  const [selectedStatus, setSelectedStatus] = useState<ReadingStatus | null>(
    initialStatus ? (parseInt(initialStatus, 10) as ReadingStatus) : null
  );
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [orderBy] = useState<BookOrderBy>(BookOrderBy.Added);
  const [ascending] = useState(false);

  // Debounce search term
  const debouncedSearch = useDebounce(searchQuery, 300);

  // Determine if we're in search mode
  const hasSearchOrFilter = debouncedSearch.trim().length > 0 || selectedStatus !== null;

  // Fetch books (regular or search)
  const { data: searchData, isLoading: isSearching } = useSearchBooks({
    searchTerm: debouncedSearch,
    status: selectedStatus,
    page,
    pageSize,
  });

  const { data: allBooks, isLoading: isLoadingAll } = useBooks({
    page,
    pageSize,
    orderBy,
    ascending,
  });

  // Use the appropriate data source
  const books = hasSearchOrFilter ? searchData?.items : allBooks?.items;
  const totalCount = hasSearchOrFilter ? searchData?.totalCount : allBooks?.totalCount;
  const totalPages = hasSearchOrFilter ? searchData?.totalPages : allBooks?.totalPages;
  const isLoading = hasSearchOrFilter ? isSearching : isLoadingAll;

  // Update URL params when search/filter changes
  useEffect(() => {
    const params: Record<string, string> = {};
    if (debouncedSearch) params.search = debouncedSearch;
    if (selectedStatus !== null) params.status = selectedStatus.toString();
    if (page !== 1) params.page = page.toString();
    if (pageSize !== 24) params.pageSize = pageSize.toString();

    setSearchParams(params, { replace: true });
  }, [debouncedSearch, selectedStatus, page, pageSize, setSearchParams]);

  // Handlers
  const handleSearchChange = (value: string) => {
    setSearchQuery(value);
    setPage(1); // Reset to first page on search
  };

  const handleStatusChange = (status: ReadingStatus | null) => {
    setSelectedStatus(status);
    setPage(1); // Reset to first page on filter change
  };

  const handlePageChange = (newPage: number) => {
    setPage(newPage);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const handlePageSizeChange = (newPageSize: number) => {
    setPageSize(newPageSize);
    setPage(1);
  };

  const handleClearFilters = () => {
    setSearchQuery("");
    setSelectedStatus(null);
    setPage(1);
  };

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <header className="border-b border-border p-6">
        <div className="flex flex-col gap-4 mb-6">
          <div className="flex items-center justify-between gap-4">
            <div className="flex-1 max-w-2xl">
              <SearchBar value={searchQuery} onChange={handleSearchChange} />
            </div>
            <Tabs
              value={selectedStatus?.toString() ?? "all"}
              onValueChange={(v) =>
                handleStatusChange(v === "all" ? null : (Number(v) as ReadingStatus))
              }
            >
              <TabsList>
                <TabsTrigger value="all">All</TabsTrigger>
                <TabsTrigger value="0">Unread</TabsTrigger>
                <TabsTrigger value="1">Reading</TabsTrigger>
                <TabsTrigger value="2">Read</TabsTrigger>
              </TabsList>
            </Tabs>
          </div>

          {hasSearchOrFilter && (
            <div className="flex justify-end">
              <Button variant="outline" onClick={handleClearFilters} size="sm">
                <X className="h-4 w-4 mr-2" />
                Clear Filters
              </Button>
            </div>
          )}
        </div>

        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-foreground mb-1">Library</h1>
            <p className="text-muted-foreground">
              {totalCount ?? 0} {totalCount === 1 ? "book" : "books"}{" "}
              {hasSearchOrFilter && "found"}
            </p>
          </div>
        </div>
      </header>

      {/* Books Grid */}
      <div className="flex-1 overflow-auto">
        <div className="p-6">
          {isLoading ? (
            <BooksGridSkeleton />
          ) : books && books.length > 0 ? (
            <>
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-6">
                {books.map((book) => (
                  <BookCard key={book.id} book={book} />
                ))}
              </div>

              {/* Pagination */}
              {totalPages && totalPages > 1 && (
                <div className="mt-8">
                  <BooksPagination
                    currentPage={page}
                    totalPages={totalPages}
                    onPageChange={handlePageChange}
                    pageSize={pageSize}
                    onPageSizeChange={handlePageSizeChange}
                    totalCount={totalCount ?? 0}
                  />
                </div>
              )}
            </>
          ) : hasSearchOrFilter ? (
            <SearchEmptyState
              searchTerm={debouncedSearch}
              onClear={handleClearFilters}
            />
          ) : (
            <EmptyState />
          )}
        </div>
      </div>
    </div>
  );
};

export default Library;
