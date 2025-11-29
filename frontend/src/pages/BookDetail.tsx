import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { ArrowLeft, BookOpen, Calendar, Trash2, Edit, Save, X } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
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
import { useBook } from '@/hooks/books/useBook';
import { useDeleteBook } from '@/hooks/books/useDeleteBook';
import { useUpdateBook } from '@/hooks/books/useUpdateBook';
import { useBookCoverColor } from '@/hooks/utils/useBookCoverColor';
import StatusBadge from '@/components/books/StatusBadge';

const BookDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: book, isLoading } = useBook(id!);
  const { mutate: deleteBook } = useDeleteBook();
  const { mutate: updateBook } = useUpdateBook();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  // Edit form state
  const [editForm, setEditForm] = useState({
    title: '',
    authors: '',
    publisher: '',
    publicationDate: '',
    isbn10: '',
    isbn13: '',
    pages: '',
    description: '',
    annotation: '',
  });

  // Extract dominant color from book cover
  const dominantColor = useBookCoverColor(book?.coverImageUrl);

  // Sync form with book data
  useEffect(() => {
    if (book) {
      setEditForm({
        title: book.title || '',
        authors: book.authors || '',
        publisher: book.publisher || '',
        publicationDate: book.publicationDate || '',
        isbn10: book.isbn10 || '',
        isbn13: book.isbn13 || '',
        pages: book.pages?.toString() || '',
        description: book.description || '',
        annotation: book.annotation || '',
      });
    }
  }, [book]);

  const handleDelete = () => {
    deleteBook(id!, {
      onSuccess: () => {
        navigate('/');
      },
    });
  };

  const handleSave = () => {
    updateBook({
      id: id!,
      data: {
        title: editForm.title,
        authors: editForm.authors,
        publisher: editForm.publisher || undefined,
        publicationDate: editForm.publicationDate || undefined,
        isbn10: editForm.isbn10 || undefined,
        isbn13: editForm.isbn13 || undefined,
        pages: editForm.pages ? parseInt(editForm.pages, 10) : undefined,
        description: editForm.description || undefined,
        annotation: editForm.annotation || undefined,
        status: book!.status,
      },
    }, {
      onSuccess: () => {
        setIsEditing(false);
      },
    });
  };

  const handleCancel = () => {
    if (book) {
      setEditForm({
        title: book.title || '',
        authors: book.authors || '',
        publisher: book.publisher || '',
        publicationDate: book.publicationDate || '',
        isbn10: book.isbn10 || '',
        isbn13: book.isbn13 || '',
        pages: book.pages?.toString() || '',
        description: book.description || '',
        annotation: book.annotation || '',
      });
    }
    setIsEditing(false);
  };

  if (isLoading) {
    return (
      <div className='h-full overflow-auto'>
        <div className='max-w-5xl mx-auto p-6'>
          <Skeleton className='h-10 w-32 mb-8' />
          <div className='grid md:grid-cols-[280px_1fr] gap-12'>
            <div>
              <Skeleton className='aspect-[2/3] w-full' />
              <div className='flex gap-2 mt-6'>
                <Skeleton className='h-9 flex-1' />
                <Skeleton className='h-9 flex-1' />
              </div>
            </div>
            <div className='space-y-6'>
              <Skeleton className='h-16 w-full' />
              <Skeleton className='h-12 w-3/4' />
              <Skeleton className='h-64 w-full' />
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!book) {
    return (
      <div className='h-full flex items-center justify-center'>
        <div className='text-center'>
          <h2 className='text-2xl font-bold text-foreground mb-2'>Book not found</h2>
          <Button onClick={() => navigate("/")}>Back to Library</Button>
        </div>
      </div>
    );
  }

  return (
    <>
      <div
        className='h-full overflow-auto relative transition-all duration-700'
        style={{
          '--book-color': dominantColor,
          background: `radial-gradient(circle at 20% 30%, hsl(var(--book-color) / 0.15), transparent 50%),
                       radial-gradient(circle at 80% 70%, hsl(var(--book-color) / 0.1), transparent 60%),
                       hsl(var(--background))`
        } as React.CSSProperties}
      >
        <div className='max-w-5xl mx-auto p-6 relative z-10'>
          <Button
            variant='ghost'
            onClick={() => navigate("/")}
            className='mb-8'
          >
            <ArrowLeft className='w-4 h-4 mr-2' />
            Back
          </Button>

          <div className='grid md:grid-cols-[280px_1fr] gap-12'>
            {/* Book Cover */}
            <div>
              <Card className='overflow-hidden bg-card/50 backdrop-blur-sm border-border/50 shadow-2xl'>
                <div className='aspect-[2/3] relative'>
                  {book.coverImageUrl ? (
                    <img
                      src={book.coverImageUrl}
                      alt={book.title}
                      className='w-full h-full object-cover'
                    />
                  ) : (
                    <div className='w-full h-full flex items-center justify-center bg-secondary'>
                      <BookOpen className='w-16 h-16 text-muted-foreground' />
                    </div>
                  )}
                </div>
              </Card>

              {/* Action Buttons */}
              {isEditing ? (
                <div className='flex gap-2 mt-6'>
                  <Button
                    variant='outline'
                    size='sm'
                    className='flex-1'
                    onClick={handleSave}
                  >
                    <Save className='w-4 h-4' />
                  </Button>
                  <Button
                    variant='outline'
                    size='sm'
                    className='flex-1'
                    onClick={handleCancel}
                  >
                    <X className='w-4 h-4' />
                  </Button>
                </div>
              ) : (
                <div className='flex gap-2 mt-6'>
                  <Button
                    variant='outline'
                    size='sm'
                    className='flex-1'
                    onClick={() => setIsEditing(true)}
                  >
                    <Edit className='w-4 h-4' />
                  </Button>
                  <Button
                    variant='outline'
                    size='sm'
                    className='flex-1'
                    onClick={() => setShowDeleteDialog(true)}
                  >
                    <Trash2 className='w-4 h-4' />
                  </Button>
                </div>
              )}
            </div>

            {/* Book Details */}
            <div className='space-y-6'>
              {isEditing ? (
                /* Edit Mode - Form Fields */
                <div className='space-y-4'>
                  <div>
                    <Label htmlFor='title'>Title</Label>
                    <Input
                      id='title'
                      value={editForm.title}
                      onChange={(e) => setEditForm({ ...editForm, title: e.target.value })}
                      className='text-2xl font-bold mt-1'
                    />
                  </div>

                  <div>
                    <Label htmlFor='authors'>Author(s)</Label>
                    <Input
                      id='authors'
                      value={editForm.authors}
                      onChange={(e) => setEditForm({ ...editForm, authors: e.target.value })}
                      className='text-lg mt-1'
                    />
                  </div>

                  <div>
                    <Label htmlFor='publisher'>Publisher</Label>
                    <Input
                      id='publisher'
                      value={editForm.publisher}
                      onChange={(e) => setEditForm({ ...editForm, publisher: e.target.value })}
                      className='mt-1'
                    />
                  </div>

                  <div>
                    <Label htmlFor='publicationDate'>Publication Date</Label>
                    <Input
                      id='publicationDate'
                      type='date'
                      value={editForm.publicationDate}
                      onChange={(e) => setEditForm({ ...editForm, publicationDate: e.target.value })}
                      className='mt-1'
                    />
                  </div>

                  <div>
                    <Label htmlFor='isbn10'>ISBN-10</Label>
                    <Input
                      id='isbn10'
                      value={editForm.isbn10}
                      onChange={(e) => setEditForm({ ...editForm, isbn10: e.target.value })}
                      className='mt-1'
                    />
                  </div>

                  <div>
                    <Label htmlFor='isbn13'>ISBN-13</Label>
                    <Input
                      id='isbn13'
                      value={editForm.isbn13}
                      onChange={(e) => setEditForm({ ...editForm, isbn13: e.target.value })}
                      className='mt-1'
                    />
                  </div>

                  <div>
                    <Label htmlFor='pages'>Pages</Label>
                    <Input
                      id='pages'
                      type='number'
                      value={editForm.pages}
                      onChange={(e) => setEditForm({ ...editForm, pages: e.target.value })}
                      className='mt-1'
                    />
                  </div>

                  <div>
                    <Label htmlFor='description'>Description</Label>
                    <Textarea
                      id='description'
                      value={editForm.description}
                      onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                      className='mt-1 min-h-[120px]'
                    />
                  </div>

                  <div>
                    <Label htmlFor='annotation'>Personal Notes</Label>
                    <Textarea
                      id='annotation'
                      value={editForm.annotation}
                      onChange={(e) => setEditForm({ ...editForm, annotation: e.target.value })}
                      className='mt-1 min-h-[120px]'
                    />
                  </div>
                </div>
              ) : (
                /* View Mode - Display Only */
                <>
                  <div>
                    <h1 className='text-5xl font-bold text-foreground mb-3 leading-tight'>
                      {book.title}
                    </h1>
                    <p className='text-2xl text-muted-foreground mb-6'>
                      {book.authors || 'Unknown Author'}
                    </p>

                    <div className='flex flex-wrap gap-2'>
                      <StatusBadge status={book.status} />
                      {book.publicationDate && (
                        <Badge
                          variant='secondary'
                          className='text-sm backdrop-blur-sm'
                          style={{
                            background: `hsl(var(--book-color) / 0.15)`,
                            borderColor: `hsl(var(--book-color) / 0.3)`
                          }}
                        >
                          <Calendar className='w-3 h-3 mr-1' />
                          {new Date(book.publicationDate).getFullYear()}
                        </Badge>
                      )}
                      {book.isbn10 && (
                        <Badge
                          variant='secondary'
                          className='text-sm backdrop-blur-sm'
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
                          variant='secondary'
                          className='text-sm backdrop-blur-sm'
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
                  <div className='space-y-4 text-foreground/90'>
                    <div>
                      <p className='text-sm text-muted-foreground mb-1'>Author(s)</p>
                      <p className='text-lg'>{book.authors || 'Unknown'}</p>
                    </div>

                    {book.publisher && (
                      <div>
                        <p className='text-sm text-muted-foreground mb-1'>Publisher</p>
                        <p className='text-lg'>{book.publisher}</p>
                      </div>
                    )}

                    {book.publicationDate && (
                      <div>
                        <p className='text-sm text-muted-foreground mb-1'>Publication Date</p>
                        <p className='text-lg'>
                          {new Date(book.publicationDate).toLocaleDateString()}
                        </p>
                      </div>
                    )}

                    {book.pages && (
                      <div>
                        <p className='text-sm text-muted-foreground mb-1'>Pages</p>
                        <p className='text-lg'>{book.pages}</p>
                      </div>
                    )}

                    <div>
                      <p className='text-sm text-muted-foreground mb-1'>Date Added</p>
                      <p className='text-lg'>
                        {new Date(book.createdDate).toLocaleDateString()}
                      </p>
                    </div>

                    {book.startedReadingDate && (
                      <div>
                        <p className='text-sm text-muted-foreground mb-1'>Started Reading</p>
                        <p className='text-lg'>
                          {new Date(book.startedReadingDate).toLocaleDateString()}
                        </p>
                      </div>
                    )}

                    {book.finishedReadingDate && (
                      <div>
                        <p className='text-sm text-muted-foreground mb-1'>Finished Reading</p>
                        <p className='text-lg'>
                          {new Date(book.finishedReadingDate).toLocaleDateString()}
                        </p>
                      </div>
                    )}

                    {book.tags && book.tags.length > 0 && (
                      <div>
                        <p className='text-sm text-muted-foreground mb-1'>Tags</p>
                        <div className='flex flex-wrap gap-2 mt-1'>
                          {book.tags.map((tag, index) => (
                            <Badge key={index} variant='outline'>
                              {tag}
                            </Badge>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Description */}
                  {book.description && (
                    <div className='pt-6 border-t border-border/30'>
                      <h2 className='text-lg font-semibold text-foreground mb-3'>
                        Description
                      </h2>
                      <p className='text-foreground/80 leading-relaxed whitespace-pre-wrap'>
                        {book.description}
                      </p>
                    </div>
                  )}

                  {/* Personal Notes */}
                  {book.annotation && (
                    <div className='pt-6 border-t border-border/30'>
                      <h2 className='text-lg font-semibold text-foreground mb-3'>
                        Personal Notes
                      </h2>
                      <p className='text-foreground/80 leading-relaxed whitespace-pre-wrap'>
                        {book.annotation}
                      </p>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      </div>

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
