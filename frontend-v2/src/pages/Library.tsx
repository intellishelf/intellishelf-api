import { useState } from "react";
import SearchBar from "@/components/SearchBar";
import BookCard from "@/components/BookCard";
import { Button } from "@/components/ui/button";
import { Grid3x3, List, Filter } from "lucide-react";
import { Book } from "@/types/book";

// Mock data
const mockBooks: Book[] = [
  {
    id: "1",
    title: "The Great Gatsby",
    author: "F. Scott Fitzgerald",
    cover: "https://covers.openlibrary.org/b/id/7222246-L.jpg",
    year: 1925,
    addedDate: "2025-01-15",
  },
  {
    id: "2",
    title: "To Kill a Mockingbird",
    author: "Harper Lee",
    cover: "https://covers.openlibrary.org/b/id/8228691-L.jpg",
    year: 1960,
    addedDate: "2025-01-10",
  },
  {
    id: "3",
    title: "1984",
    author: "George Orwell",
    cover: "https://covers.openlibrary.org/b/id/7222246-L.jpg",
    year: 1949,
    addedDate: "2025-01-05",
  },
];

const Library = () => {
  const [searchQuery, setSearchQuery] = useState("");
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");

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

      {/* Books Grid */}
      <div className="flex-1 overflow-auto p-6">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-6">
          {filteredBooks.map((book) => (
            <BookCard key={book.id} book={book} />
          ))}
        </div>

        {filteredBooks.length === 0 && (
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
