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
import StatusBadge from '@/components/books/StatusBadge';
import BookForm from '@/components/books/BookForm';

const BookDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: book, isLoading } = useBook(id!);
  const { mutate: deleteBook } = useDeleteBook();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);

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
          <Skeleton className="h-10 w-32 mb-6" />
          <div className="grid md:grid-cols-[300px_1fr] gap-8">
            <div>
              <Skeleton className="aspect-[2/3] w-full" />
              <div className="flex gap-2 mt-4">
                <Skeleton className="h-10 flex-1" />
                <Skeleton className="h-10 flex-1" />
              </div>
            </div>
            <div className="space-y-6">
              <Skeleton className="h-32 w-full" />
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
                  {book.coverImageUrl ? (
                    <img
                      src={book.coverImageUrl}
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
                <Button
                  variant="outline"
                  className="flex-1"
                  onClick={() => setShowEditDialog(true)}
                >
                  <Edit className="w-4 h-4 mr-2" />
                  Edit
                </Button>
                <Button
                  variant="outline"
                  className="flex-1"
                  onClick={() => setShowDeleteDialog(true)}
                >
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
                <p className="text-xl text-muted-foreground mb-4">
                  {book.authors || 'Unknown Author'}
                </p>

                <div className="flex flex-wrap gap-2">
                  <StatusBadge status={book.status} />
                  {book.publicationDate && (
                    <Badge variant="secondary" className="text-sm">
                      <Calendar className="w-3 h-3 mr-1" />
                      {new Date(book.publicationDate).getFullYear()}
                    </Badge>
                  )}
                  {book.isbn10 && (
                    <Badge variant="secondary" className="text-sm">
                      ISBN-10: {book.isbn10}
                    </Badge>
                  )}
                  {book.isbn13 && (
                    <Badge variant="secondary" className="text-sm">
                      ISBN-13: {book.isbn13}
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
                      Author(s)
                    </label>
                    <p className="text-foreground">{book.authors || 'Unknown'}</p>
                  </div>

                  {book.publisher && (
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Publisher
                      </label>
                      <p className="text-foreground">{book.publisher}</p>
                    </div>
                  )}

                  {book.publicationDate && (
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Publication Date
                      </label>
                      <p className="text-foreground">
                        {new Date(book.publicationDate).toLocaleDateString()}
                      </p>
                    </div>
                  )}

                  {book.pages && (
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Pages
                      </label>
                      <p className="text-foreground">{book.pages}</p>
                    </div>
                  )}

                  <div>
                    <label className="text-sm font-medium text-muted-foreground">
                      Date Added
                    </label>
                    <p className="text-foreground">
                      {new Date(book.createdDate).toLocaleDateString()}
                    </p>
                  </div>

                  {book.startedReadingDate && (
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Started Reading
                      </label>
                      <p className="text-foreground">
                        {new Date(book.startedReadingDate).toLocaleDateString()}
                      </p>
                    </div>
                  )}

                  {book.finishedReadingDate && (
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Finished Reading
                      </label>
                      <p className="text-foreground">
                        {new Date(book.finishedReadingDate).toLocaleDateString()}
                      </p>
                    </div>
                  )}

                  {book.tags && book.tags.length > 0 && (
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Tags
                      </label>
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
              </Card>

              {/* Description Card */}
              {book.description && (
                <Card className="bg-gradient-to-b from-card to-background border-border p-6 mb-6">
                  <h2 className="text-lg font-semibold text-foreground mb-4">
                    Description
                  </h2>
                  <p className="text-foreground whitespace-pre-wrap">{book.description}</p>
                </Card>
              )}

              {/* Notes Card */}
              {book.annotation && (
                <Card className="bg-gradient-to-b from-card to-background border-border p-6">
                  <h2 className="text-lg font-semibold text-foreground mb-4">
                    Personal Notes
                  </h2>
                  <p className="text-foreground whitespace-pre-wrap">{book.annotation}</p>
                </Card>
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
