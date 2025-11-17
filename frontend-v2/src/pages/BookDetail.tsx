import { useState } from 'react';
import { useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ArrowLeft, BookOpen, Calendar, Trash2, Edit } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
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
import { useBook } from '@/hooks/books/useBook';
import { useDeleteBook } from '@/hooks/books/useDeleteBook';
import { useBookCoverColor } from '@/hooks/utils/useBookCoverColor';
import StatusBadge from '@/components/books/StatusBadge';
import BookForm from '@/components/books/BookForm';

const BookDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: book, isLoading } = useBook(id!);
  const { mutate: deleteBook } = useDeleteBook();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);

  // Extract dominant color from book cover
  const dominantColor = useBookCoverColor(book?.coverImageUrl);

  const handleDelete = () => {
    deleteBook(id!, {
      onSuccess: () => {
        navigate('/');
      },
    });
  };

  if (isLoading) {
    return (
      <div className="h-full overflow-auto">
        <div className="max-w-5xl mx-auto p-6">
          <Skeleton className="h-10 w-32 mb-8" />
          <div className="grid md:grid-cols-[280px_1fr] gap-12">
            <div>
              <Skeleton className="aspect-[2/3] w-full" />
              <div className="flex gap-2 mt-6">
                <Skeleton className="h-9 flex-1" />
                <Skeleton className="h-9 flex-1" />
              </div>
            </div>
            <div className="space-y-6">
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-12 w-3/4" />
              <Skeleton className="h-64 w-full" />
            </div>
          </div>
        </div>
      </div>
    );
  }

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
    <>
      <div
        className="h-full overflow-auto relative transition-all duration-700"
        style={{
          '--book-color': dominantColor,
          background: `radial-gradient(circle at 20% 30%, hsl(var(--book-color) / 0.15), transparent 50%),
                       radial-gradient(circle at 80% 70%, hsl(var(--book-color) / 0.1), transparent 60%),
                       hsl(var(--background))`
        } as React.CSSProperties}
      >
        <div className="max-w-5xl mx-auto p-6 relative z-10">
          <Button
            variant="ghost"
            onClick={() => navigate("/")}
            className="mb-8"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back
          </Button>

          <div className="grid md:grid-cols-[280px_1fr] gap-12">
            {/* Book Cover */}
            <div>
              <Card className="overflow-hidden bg-card/50 backdrop-blur-sm border-border/50 shadow-2xl">
                <div className="aspect-[2/3] relative">
                  {book.coverImageUrl ? (
                    <img
                      src={book.coverImageUrl}
                      alt={book.title}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center bg-secondary">
                      <BookOpen className="w-16 h-16 text-muted-foreground" />
                    </div>
                  )}
                </div>
              </Card>

              {/* Action Buttons */}
              <div className="flex gap-2 mt-6">
                <Button
                  variant="outline"
                  size="sm"
                  className="flex-1"
                  onClick={() => setShowEditDialog(true)}
                >
                  <Edit className="w-4 h-4" />
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="flex-1"
                  onClick={() => setShowDeleteDialog(true)}
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              </div>
            </div>

            {/* Book Details */}
            <div className="space-y-6">
              <div>
                <h1 className="text-5xl font-bold text-foreground mb-3 leading-tight">
                  {book.title}
                </h1>
                <p className="text-2xl text-muted-foreground mb-6">
                  {book.authors || 'Unknown Author'}
                </p>

                <div className="flex flex-wrap gap-2">
                  <StatusBadge status={book.status} />
                  {book.publicationDate && (
                    <Badge
                      variant="secondary"
                      className="text-sm backdrop-blur-sm"
                      style={{
                        background: `hsl(var(--book-color) / 0.15)`,
                        borderColor: `hsl(var(--book-color) / 0.3)`
                      }}
                    >
                      <Calendar className="w-3 h-3 mr-1" />
                      {new Date(book.publicationDate).getFullYear()}
                    </Badge>
                  )}
                  {book.isbn10 && (
                    <Badge
                      variant="secondary"
                      className="text-sm backdrop-blur-sm"
                      style={{
                        background: `hsl(var(--book-color) / 0.15)`,
                        borderColor: `hsl(var(--book-color) / 0.3)`
                      }}
                    >
                      ISBN-10: {book.isbn10}
                    </Badge>
                  )}
                  {book.isbn13 && (
                    <Badge
                      variant="secondary"
                      className="text-sm backdrop-blur-sm"
                      style={{
                        background: `hsl(var(--book-color) / 0.15)`,
                        borderColor: `hsl(var(--book-color) / 0.3)`
                      }}
                    >
                      ISBN-13: {book.isbn13}
                    </Badge>
                  )}
                </div>
              </div>

              {/* Information */}
              <div className="space-y-4 text-foreground/90">
                <div>
                  <p className="text-sm text-muted-foreground mb-1">Author(s)</p>
                  <p className="text-lg">{book.authors || 'Unknown'}</p>
                </div>

                {book.publisher && (
                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Publisher</p>
                    <p className="text-lg">{book.publisher}</p>
                  </div>
                )}

                {book.publicationDate && (
                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Publication Date</p>
                    <p className="text-lg">
                      {new Date(book.publicationDate).toLocaleDateString()}
                    </p>
                  </div>
                )}

                {book.pages && (
                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Pages</p>
                    <p className="text-lg">{book.pages}</p>
                  </div>
                )}

                <div>
                  <p className="text-sm text-muted-foreground mb-1">Date Added</p>
                  <p className="text-lg">
                    {new Date(book.createdDate).toLocaleDateString()}
                  </p>
                </div>

                {book.startedReadingDate && (
                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Started Reading</p>
                    <p className="text-lg">
                      {new Date(book.startedReadingDate).toLocaleDateString()}
                    </p>
                  </div>
                )}

                {book.finishedReadingDate && (
                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Finished Reading</p>
                    <p className="text-lg">
                      {new Date(book.finishedReadingDate).toLocaleDateString()}
                    </p>
                  </div>
                )}

                {book.tags && book.tags.length > 0 && (
                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Tags</p>
                    <div className="flex flex-wrap gap-2 mt-1">
                      {book.tags.map((tag, index) => (
                        <Badge key={index} variant="outline">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {/* Description */}
              {book.description && (
                <div className="pt-6 border-t border-border/30">
                  <h2 className="text-lg font-semibold text-foreground mb-3">
                    Description
                  </h2>
                  <p className="text-foreground/80 leading-relaxed whitespace-pre-wrap">
                    {book.description}
                  </p>
                </div>
              )}

              {/* Personal Notes */}
              {book.annotation && (
                <div className="pt-6 border-t border-border/30">
                  <h2 className="text-lg font-semibold text-foreground mb-3">
                    Personal Notes
                  </h2>
                  <p className="text-foreground/80 leading-relaxed whitespace-pre-wrap">
                    {book.annotation}
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Edit Dialog */}
      <Dialog open={showEditDialog} onOpenChange={setShowEditDialog}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit Book</DialogTitle>
          </DialogHeader>
          <BookForm
            book={book}
            onSuccess={() => setShowEditDialog(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Delete Dialog */}
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
            <AlertDialogAction onClick={handleDelete}>
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
};

export default BookDetail;
