import { useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Book } from '@/types/book';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useDeleteBook } from '@/hooks/books/useDeleteBook';
import { useBookCoverColor } from '@/hooks/utils/useBookCoverColor';
import BookForm from './books/BookForm';
import StatusBadge from './books/StatusBadge';

interface BookCardProps {
  book: Book;
  onClick?: () => void;
  viewMode?: 'grid' | 'list';
}

const BookCard = ({ book, onClick, viewMode = 'grid' }: BookCardProps) => {
  const navigate = useNavigate();
  const { mutate: deleteBook } = useDeleteBook();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);

  // Extract dominant color for subtle accent
  const dominantColor = useBookCoverColor(book.coverImageUrl);

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else {
      navigate(`/book/${book.id}`);
    }
  };

  const handleEdit = (e: React.MouseEvent) => {
    e.stopPropagation();
    setShowEditDialog(true);
  };

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    setShowDeleteDialog(true);
  };

  const confirmDelete = () => {
    deleteBook(book.id);
    setShowDeleteDialog(false);
  };

  // List view layout
  if (viewMode === 'list') {
    return (
      <>
        <Card
          className='bg-book-card hover:bg-book-card-hover border-border transition-all cursor-pointer group overflow-hidden'
          onClick={handleClick}
        >
          <div className='flex gap-4 p-4'>
            <div className='w-24 h-36 relative overflow-hidden bg-secondary rounded flex-shrink-0'>
              {book.coverImageUrl ? (
                <img
                  src={book.coverImageUrl}
                  alt={book.title}
                  className='w-full h-full object-cover group-hover:scale-105 transition-transform duration-300'
                />
              ) : (
                <div className='w-full h-full flex items-center justify-center'>
                  <span className='text-muted-foreground text-xs'>No cover</span>
                </div>
              )}

              {/* Hover overlay with actions */}
              <div className='absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2'>
                <Button size='icon' variant='secondary' className='h-8 w-8' onClick={handleEdit}>
                  <Pencil className='h-4 w-4' />
                </Button>
                <Button size='icon' variant='destructive' className='h-8 w-8' onClick={handleDelete}>
                  <Trash2 className='h-4 w-4' />
                </Button>
              </div>
            </div>

            <div className='flex-1 flex flex-col justify-center min-w-0 gap-2'>
              <div>
                <h3 className='font-semibold text-foreground text-lg mb-1 line-clamp-1'>
                  {book.title}
                </h3>
                <p className='text-sm text-muted-foreground line-clamp-1'>
                  {book.authors || 'Unknown Author'}
                </p>
              </div>

              <div className='flex items-center gap-2'>
                <StatusBadge status={book.status} />
              </div>

              {book.description && (
                <p className='text-sm text-muted-foreground/80 line-clamp-2'>
                  {book.description}
                </p>
              )}

              <div className='flex items-center gap-4 text-xs text-muted-foreground mt-auto'>
                {book.publicationDate && (
                  <span>Published {new Date(book.publicationDate).getFullYear()}</span>
                )}
                <span>Added {new Date(book.createdDate).toLocaleDateString()}</span>
              </div>
            </div>
          </div>
        </Card>

        {/* Edit dialog */}
        <Dialog open={showEditDialog} onOpenChange={setShowEditDialog}>
          <DialogContent className='max-w-2xl max-h-[90vh] overflow-y-auto'>
            <DialogHeader>
              <DialogTitle>Edit Book</DialogTitle>
            </DialogHeader>
            <BookForm
              book={book}
              onSuccess={() => setShowEditDialog(false)}
            />
          </DialogContent>
        </Dialog>

        {/* Delete confirmation dialog */}
        <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Delete "{book.title}"?</AlertDialogTitle>
              <AlertDialogDescription>
                This action cannot be undone. This will permanently delete the book
                from your library.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction onClick={confirmDelete}>
                Delete
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </>
    );
  }

  // Grid view layout (default)
  return (
    <>
      <Card
        className='bg-book-card hover:bg-book-card-hover border-border transition-all duration-300 cursor-pointer group overflow-hidden relative'
        onClick={handleClick}
        style={{
          '--card-color': dominantColor,
        } as React.CSSProperties}
      >
        <div className='aspect-[2/3] relative overflow-hidden bg-secondary'>
          {book.coverImageUrl ? (
            <img
              src={book.coverImageUrl}
              alt={book.title}
              className='w-full h-full object-cover group-hover:scale-105 transition-transform duration-300'
            />
          ) : (
            <div className='w-full h-full flex items-center justify-center'>
              <span className='text-muted-foreground text-sm'>No cover</span>
            </div>
          )}

          {/* Hover overlay with actions */}
          <div className='absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2'>
            <Button size='icon' variant='secondary' className='h-9 w-9' onClick={handleEdit}>
              <Pencil className='h-4 w-4' />
            </Button>
            <Button size='icon' variant='destructive' className='h-9 w-9' onClick={handleDelete}>
              <Trash2 className='h-4 w-4' />
            </Button>
          </div>

          {/* Subtle color accent on hover */}
          <div
            className='absolute inset-0 opacity-0 group-hover:opacity-20 transition-opacity pointer-events-none'
            style={{
              background: `radial-gradient(circle at 50% 100%, hsl(var(--card-color) / 0.5), transparent 70%)`
            }}
          />
        </div>

        <div className='p-4'>
          <h3 className='font-semibold text-foreground line-clamp-2 mb-1'>
            {book.title}
          </h3>
          <p className='text-sm text-muted-foreground line-clamp-1'>
            {book.authors || 'Unknown Author'}
          </p>
          <div className='mt-2'>
            <StatusBadge status={book.status} />
          </div>
        </div>
      </Card>

      {/* Edit dialog */}
      <Dialog open={showEditDialog} onOpenChange={setShowEditDialog}>
        <DialogContent className='max-w-2xl max-h-[90vh] overflow-y-auto'>
          <DialogHeader>
            <DialogTitle>Edit Book</DialogTitle>
          </DialogHeader>
          <BookForm
            book={book}
            onSuccess={() => setShowEditDialog(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Delete confirmation dialog */}
      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete "{book.title}"?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. This will permanently delete the book
              from your library.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmDelete}>
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
};

export default BookCard;
