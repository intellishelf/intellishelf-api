import { useState, useEffect } from "react";
import SearchBar from "@/components/SearchBar";
import BookCard from "@/components/BookCard";
import { Button } from "@/components/ui/button";
import { Grid3x3, List, Filter } from "lucide-react";
import { Book } from "@/types/book";
import { Skeleton } from "@/components/ui/skeleton";

// Mock data
const mockBooks: Book[] = [
  {
    id: "1",
    title: "The Great Gatsby",
    author: "F. Scott Fitzgerald",
    cover: "https://covers.openlibrary.org/b/id/7222246-L.jpg",
    year: 1925,
    addedDate: "2025-01-15",
    description: "A tale of wealth, love, and the American Dream in the Jazz Age. Nick Carraway becomes drawn into the world of his mysterious neighbor, Jay Gatsby, and his obsession with the beautiful Daisy Buchanan.",
    tags: ["Classic", "Fiction", "Romance"],
  },
  {
    id: "2",
    title: "To Kill a Mockingbird",
    author: "Harper Lee",
    cover: "https://covers.openlibrary.org/b/id/8228691-L.jpg",
    year: 1960,
    addedDate: "2025-01-10",
    description: "Through the eyes of young Scout Finch, witness the racial injustice and moral growth in a small Alabama town as her father defends a Black man wrongly accused of a crime.",
    tags: ["Classic", "Historical", "Drama"],
  },
  {
    id: "3",
    title: "1984",
    author: "George Orwell",
    cover: "https://covers.openlibrary.org/b/id/7222246-L.jpg",
    year: 1949,
    addedDate: "2025-01-05",
    description: "A dystopian masterpiece depicting a totalitarian society where Big Brother watches everything, truth is manipulated, and independent thought is a crime punishable by death.",
    tags: ["Sci-Fi", "Dystopian", "Political"],
  },
];

const Library = () => {
  const [searchQuery, setSearchQuery] = useState("");
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Simulate loading
    const timer = setTimeout(() => {
      setIsLoading(false);
    }, 1000);
    return () => clearTimeout(timer);
  }, []);

  const filteredBooks = mockBooks.filter(
    (book) =>
      book.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      book.author.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <header className="border-b border-border p-6">
        <div className="flex items-center justify-between gap-4 mb-6">
          <SearchBar value={searchQuery} onChange={setSearchQuery} />
          <div className="flex items-center gap-2">
            <Button
              variant={viewMode === "grid" ? "default" : "outline"}
              size="icon"
              onClick={() => setViewMode("grid")}
            >
              <Grid3x3 className="w-4 h-4" />
            </Button>
            <Button
              variant={viewMode === "list" ? "default" : "outline"}
              size="icon"
              onClick={() => setViewMode("list")}
            >
              <List className="w-4 h-4" />
            </Button>
            <Button variant="outline" size="icon">
              <Filter className="w-4 h-4" />
            </Button>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-foreground mb-1">Library</h1>
            <p className="text-muted-foreground">
              {filteredBooks.length} {filteredBooks.length === 1 ? "book" : "books"}
            </p>
          </div>
        </div>
      </header>

      {/* Books Display */}
      <div className="flex-1 overflow-auto p-6">
        {isLoading ? (
          viewMode === "grid" ? (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-6">
              {Array.from({ length: 12 }).map((_, i) => (
                <div key={i} className="space-y-3">
                  <Skeleton className="aspect-[2/3] w-full rounded-lg" />
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/2" />
                </div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col gap-3 max-w-4xl">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="flex gap-4 p-4 border border-border rounded-lg">
                  <Skeleton className="w-24 h-36 rounded flex-shrink-0" />
                  <div className="flex-1 space-y-3">
                    <Skeleton className="h-5 w-3/4" />
                    <Skeleton className="h-4 w-1/2" />
                    <Skeleton className="h-4 w-full" />
                    <Skeleton className="h-4 w-full" />
                    <div className="flex gap-2">
                      <Skeleton className="h-5 w-16" />
                      <Skeleton className="h-5 w-16" />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )
        ) : viewMode === "grid" ? (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-6">
            {filteredBooks.map((book) => (
              <BookCard key={book.id} book={book} />
            ))}
          </div>
        ) : (
          <div className="flex flex-col gap-3 max-w-4xl">
            {filteredBooks.map((book) => (
              <BookCard key={book.id} book={book} viewMode="list" />
            ))}
          </div>
        )}

        {!isLoading && filteredBooks.length === 0 && (
          <div className="flex flex-col items-center justify-center h-full text-center">
            <p className="text-muted-foreground text-lg">No books found</p>
            <p className="text-muted-foreground text-sm mt-2">
              Try adjusting your search
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default Library;
