import { Book } from "@/types/book";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { useNavigate } from "react-router-dom";

interface BookCardProps {
  book: Book;
  onClick?: () => void;
  viewMode?: "grid" | "list";
}

const BookCard = ({ book, onClick, viewMode = "grid" }: BookCardProps) => {
  const navigate = useNavigate();

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else {
      navigate(`/book/${book.id}`);
    }
  };

  if (viewMode === "list") {
    return (
      <Card
        className="bg-book-card hover:bg-book-card-hover border-border transition-all cursor-pointer group overflow-hidden"
        onClick={handleClick}
      >
        <div className="flex gap-4 p-4">
          <div className="w-24 h-36 relative overflow-hidden bg-secondary rounded flex-shrink-0">
            {book.cover ? (
              <img
                src={book.cover}
                alt={book.title}
                className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center">
                <span className="text-muted-foreground text-xs">No cover</span>
              </div>
            )}
          </div>
          <div className="flex-1 flex flex-col justify-center min-w-0 gap-2">
            <div>
              <h3 className="font-semibold text-foreground text-lg mb-1 line-clamp-1">
                {book.title}
              </h3>
              <p className="text-sm text-muted-foreground line-clamp-1">
                {book.author}
              </p>
            </div>
            {book.tags && book.tags.length > 0 && (
              <div className="flex flex-wrap gap-1.5">
                {book.tags.map((tag) => (
                  <Badge key={tag} variant="secondary" className="text-xs px-2 py-0.5">
                    {tag}
                  </Badge>
                ))}
              </div>
            )}
            {book.description && (
              <p className="text-sm text-muted-foreground/80 line-clamp-2 relative">
                {book.description}
              </p>
            )}
            <div className="flex items-center gap-4 text-xs text-muted-foreground mt-auto">
              {book.year && <span>Published {book.year}</span>}
              {book.addedDate && <span>Added {new Date(book.addedDate).toLocaleDateString()}</span>}
            </div>
          </div>
        </div>
      </Card>
    );
  }

  return (
    <Card
      className="bg-book-card hover:bg-book-card-hover border-border transition-all cursor-pointer group overflow-hidden"
      onClick={handleClick}
    >
      <div className="aspect-[2/3] relative overflow-hidden bg-secondary">
        {book.cover ? (
          <img
            src={book.cover}
            alt={book.title}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <span className="text-muted-foreground text-sm">No cover</span>
          </div>
        )}
      </div>
      <div className="p-4 space-y-2">
        <h3 className="font-semibold text-foreground line-clamp-2 mb-1">
          {book.title}
        </h3>
        <p className="text-sm text-muted-foreground line-clamp-1">
          {book.author}
        </p>
        {book.tags && book.tags.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {book.tags.slice(0, 2).map((tag) => (
              <Badge key={tag} variant="secondary" className="text-xs px-1.5 py-0">
                {tag}
              </Badge>
            ))}
          </div>
        )}
      </div>
    </Card>
  );
};

export default BookCard;
