import { useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ArrowLeft, BookOpen, Calendar, Trash2, Edit } from "lucide-react";

// Mock data - in production this would come from a database
const mockBooks = [
  {
    id: "1",
    title: "The Great Gatsby",
    author: "F. Scott Fitzgerald",
    cover: "https://covers.openlibrary.org/b/id/7222246-L.jpg",
    year: 1925,
    addedDate: "2025-01-15",
    isbn: "978-0-7432-7356-5",
    notes: "A classic American novel about the Jazz Age.",
  },
  {
    id: "2",
    title: "To Kill a Mockingbird",
    author: "Harper Lee",
    cover: "https://covers.openlibrary.org/b/id/8228691-L.jpg",
    year: 1960,
    addedDate: "2025-01-10",
    isbn: "978-0-06-112008-4",
    notes: "A powerful story about racial injustice and childhood innocence.",
  },
  {
    id: "3",
    title: "1984",
    author: "George Orwell",
    cover: "https://covers.openlibrary.org/b/id/7222246-L.jpg",
    year: 1949,
    addedDate: "2025-01-05",
    isbn: "978-0-452-28423-4",
    notes: "Dystopian novel about totalitarianism and surveillance.",
  },
];

const BookDetail = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  
  const book = mockBooks.find((b) => b.id === id);

  if (!book) {
    return (
      <div className="h-full flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-foreground mb-2">Book not found</h2>
          <Button onClick={() => navigate("/")}>Back to Library</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <div className="max-w-5xl mx-auto p-6">
        {/* Back Button */}
        <Button
          variant="ghost"
          onClick={() => navigate("/")}
          className="mb-6"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Library
        </Button>

        <div className="grid md:grid-cols-[300px_1fr] gap-8">
          {/* Book Cover */}
          <div>
            <Card className="overflow-hidden bg-card border-border">
              <div className="aspect-[2/3] relative bg-secondary">
                {book.cover ? (
                  <img
                    src={book.cover}
                    alt={book.title}
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <div className="w-full h-full flex items-center justify-center">
                    <BookOpen className="w-16 h-16 text-muted-foreground" />
                  </div>
                )}
              </div>
            </Card>

            {/* Action Buttons */}
            <div className="flex gap-2 mt-4">
              <Button variant="outline" className="flex-1">
                <Edit className="w-4 h-4 mr-2" />
                Edit
              </Button>
              <Button variant="outline" className="flex-1">
                <Trash2 className="w-4 h-4 mr-2" />
                Delete
              </Button>
            </div>
          </div>

          {/* Book Details */}
          <div>
            <div className="mb-6 p-6 rounded-lg bg-gradient-to-br from-primary/10 to-primary/5 border border-primary/20">
              <h1 className="text-4xl font-bold text-foreground mb-2">
                {book.title}
              </h1>
              <p className="text-xl text-muted-foreground mb-4">{book.author}</p>
              
              <div className="flex flex-wrap gap-2">
                {book.year && (
                  <Badge variant="secondary" className="text-sm">
                    <Calendar className="w-3 h-3 mr-1" />
                    {book.year}
                  </Badge>
                )}
                {book.isbn && (
                  <Badge variant="secondary" className="text-sm">
                    ISBN: {book.isbn}
                  </Badge>
                )}
              </div>
            </div>

            {/* Details Card */}
            <Card className="bg-gradient-to-b from-card to-background border-border p-6 mb-6">
              <h2 className="text-lg font-semibold text-foreground mb-4">
                Book Information
              </h2>
              
              <div className="space-y-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">
                    Author
                  </label>
                  <p className="text-foreground">{book.author}</p>
                </div>

                {book.year && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">
                      Publication Year
                    </label>
                    <p className="text-foreground">{book.year}</p>
                  </div>
                )}

                {book.isbn && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">
                      ISBN
                    </label>
                    <p className="text-foreground">{book.isbn}</p>
                  </div>
                )}

                <div>
                  <label className="text-sm font-medium text-muted-foreground">
                    Date Added
                  </label>
                  <p className="text-foreground">
                    {new Date(book.addedDate).toLocaleDateString()}
                  </p>
                </div>
              </div>
            </Card>

            {/* Notes Card */}
            {book.notes && (
              <Card className="bg-gradient-to-b from-card to-background border-border p-6">
                <h2 className="text-lg font-semibold text-foreground mb-4">
                  Notes
                </h2>
                <p className="text-foreground whitespace-pre-wrap">{book.notes}</p>
              </Card>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default BookDetail;
