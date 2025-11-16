import { Book } from "@/types/book";
import { Card } from "@/components/ui/card";
import { useNavigate } from "react-router-dom";

interface BookCardProps {
  book: Book;
  onClick?: () => void;
}

const BookCard = ({ book, onClick }: BookCardProps) => {
  const navigate = useNavigate();

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else {
      navigate(`/book/${book.id}`);
    }
  };

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
      <div className="p-4">
        <h3 className="font-semibold text-foreground line-clamp-2 mb-1">
          {book.title}
        </h3>
        <p className="text-sm text-muted-foreground line-clamp-1">
          {book.author}
        </p>
      </div>
    </Card>
  );
};

export default BookCard;
