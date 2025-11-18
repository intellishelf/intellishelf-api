import { useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { ArrowLeft, BookOpen, Calendar, Trash2, Edit, Save, X } from "lucide-react";
import { useState, useEffect } from "react";
import { useToast } from "@/hooks/use-toast";

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
  const { toast } = useToast();
  const [dominantColor, setDominantColor] = useState<string>("240 10% 50%");
  const [isEditing, setIsEditing] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  
  const book = mockBooks.find((b) => b.id === id);
  
  const [editForm, setEditForm] = useState({
    title: book?.title || "",
    author: book?.author || "",
    year: book?.year || "",
    isbn: book?.isbn || "",
    notes: book?.notes || "",
  });

  useEffect(() => {
    if (book) {
      setEditForm({
        title: book.title,
        author: book.author,
        year: book.year || "",
        isbn: book.isbn || "",
        notes: book.notes || "",
      });
    }
  }, [book]);

  useEffect(() => {
    if (book?.cover) {
      const img = new Image();
      img.crossOrigin = "Anonymous";
      img.src = book.cover;
      
      img.onload = () => {
        const canvas = document.createElement("canvas");
        const ctx = canvas.getContext("2d");
        if (!ctx) return;

        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);

        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;
        
        let r = 0, g = 0, b = 0, count = 0;
        for (let i = 0; i < data.length; i += 4) {
          r += data[i];
          g += data[i + 1];
          b += data[i + 2];
          count++;
        }
        
        r = Math.floor(r / count);
        g = Math.floor(g / count);
        b = Math.floor(b / count);
        
        const hsl = rgbToHsl(r, g, b);
        setDominantColor(`${hsl.h} ${hsl.s}% ${hsl.l}%`);
      };
    }
  }, [book?.cover]);

  const rgbToHsl = (r: number, g: number, b: number) => {
    r /= 255;
    g /= 255;
    b /= 255;
    
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    let h = 0, s = 0, l = (max + min) / 2;

    if (max !== min) {
      const d = max - min;
      s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
      
      switch (max) {
        case r: h = ((g - b) / d + (g < b ? 6 : 0)) / 6; break;
        case g: h = ((b - r) / d + 2) / 6; break;
        case b: h = ((r - g) / d + 4) / 6; break;
      }
    }
    
    return {
      h: Math.round(h * 360),
      s: Math.round(s * 100),
      l: Math.round(l * 100)
    };
  };

  const handleSave = () => {
    // In production, this would save to database
    toast({
      title: "Book updated",
      description: "Your changes have been saved successfully.",
    });
    setIsEditing(false);
  };

  const handleDelete = () => {
    // In production, this would delete from database
    toast({
      title: "Book deleted",
      description: "The book has been removed from your library.",
    });
    navigate("/");
  };

  const handleCancel = () => {
    if (book) {
      setEditForm({
        title: book.title,
        author: book.author,
        year: book.year || "",
        isbn: book.isbn || "",
        notes: book.notes || "",
      });
    }
    setIsEditing(false);
  };

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
    <div 
      className="h-full overflow-auto relative transition-all duration-700"
      style={{
        background: `radial-gradient(circle at 20% 30%, hsl(${dominantColor} / 0.15), transparent 50%), radial-gradient(circle at 80% 70%, hsl(${dominantColor} / 0.1), transparent 60%), hsl(var(--background))`
      }}
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
          <div>
            <Card className="overflow-hidden bg-card/50 backdrop-blur-sm border-border/50 shadow-2xl">
              <div className="aspect-[2/3] relative">
                {book.cover ? (
                  <img
                    src={book.cover}
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

            {isEditing ? (
              <div className="flex gap-2 mt-6">
                <Button variant="outline" size="sm" className="flex-1" onClick={handleSave}>
                  <Save className="w-4 h-4" />
                </Button>
                <Button variant="outline" size="sm" className="flex-1" onClick={handleCancel}>
                  <X className="w-4 h-4" />
                </Button>
              </div>
            ) : (
              <div className="flex gap-2 mt-6">
                <Button variant="outline" size="sm" className="flex-1" onClick={() => setIsEditing(true)}>
                  <Edit className="w-4 h-4" />
                </Button>
                <Button variant="outline" size="sm" className="flex-1" onClick={() => setShowDeleteDialog(true)}>
                  <Trash2 className="w-4 h-4" />
                </Button>
              </div>
            )}
          </div>

          <div className="space-y-6">
            {isEditing ? (
              <div className="space-y-4">
                <div>
                  <Label htmlFor="title">Title</Label>
                  <Input
                    id="title"
                    value={editForm.title}
                    onChange={(e) => setEditForm({ ...editForm, title: e.target.value })}
                    className="text-2xl font-bold mt-1"
                  />
                </div>
                <div>
                  <Label htmlFor="author">Author</Label>
                  <Input
                    id="author"
                    value={editForm.author}
                    onChange={(e) => setEditForm({ ...editForm, author: e.target.value })}
                    className="text-lg mt-1"
                  />
                </div>
                <div>
                  <Label htmlFor="year">Publication Year</Label>
                  <Input
                    id="year"
                    type="number"
                    value={editForm.year}
                    onChange={(e) => setEditForm({ ...editForm, year: e.target.value })}
                    className="mt-1"
                  />
                </div>
                <div>
                  <Label htmlFor="isbn">ISBN</Label>
                  <Input
                    id="isbn"
                    value={editForm.isbn}
                    onChange={(e) => setEditForm({ ...editForm, isbn: e.target.value })}
                    className="mt-1"
                  />
                </div>
                <div>
                  <Label htmlFor="notes">Notes</Label>
                  <Textarea
                    id="notes"
                    value={editForm.notes}
                    onChange={(e) => setEditForm({ ...editForm, notes: e.target.value })}
                    className="mt-1 min-h-[120px]"
                  />
                </div>
              </div>
            ) : (
              <>
                <div>
                  <h1 className="text-5xl font-bold text-foreground mb-3 leading-tight">
                    {book.title}
                  </h1>
                  <p className="text-2xl text-muted-foreground mb-6">{book.author}</p>
                  
                  <div className="flex flex-wrap gap-2">
                    {book.year && (
                      <Badge 
                        variant="secondary" 
                        className="text-sm backdrop-blur-sm"
                        style={{
                          background: `hsl(${dominantColor} / 0.15)`,
                          borderColor: `hsl(${dominantColor} / 0.3)`
                        }}
                      >
                        <Calendar className="w-3 h-3 mr-1" />
                        {book.year}
                      </Badge>
                    )}
                    {book.isbn && (
                      <Badge 
                        variant="secondary" 
                        className="text-sm backdrop-blur-sm"
                        style={{
                          background: `hsl(${dominantColor} / 0.15)`,
                          borderColor: `hsl(${dominantColor} / 0.3)`
                        }}
                      >
                        ISBN: {book.isbn}
                      </Badge>
                    )}
                  </div>
                </div>

                <div className="space-y-4 text-foreground/90">
                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Author</p>
                    <p className="text-lg">{book.author}</p>
                  </div>

                  {book.year && (
                    <div>
                      <p className="text-sm text-muted-foreground mb-1">Publication Year</p>
                      <p className="text-lg">{book.year}</p>
                    </div>
                  )}

                  {book.isbn && (
                    <div>
                      <p className="text-sm text-muted-foreground mb-1">ISBN</p>
                      <p className="text-lg">{book.isbn}</p>
                    </div>
                  )}

                  <div>
                    <p className="text-sm text-muted-foreground mb-1">Date Added</p>
                    <p className="text-lg">
                      {new Date(book.addedDate).toLocaleDateString()}
                    </p>
                  </div>
                </div>

                {book.notes && (
                  <div className="pt-6 border-t border-border/30">
                    <h2 className="text-lg font-semibold text-foreground mb-3">
                      Notes
                    </h2>
                    <p className="text-foreground/80 leading-relaxed whitespace-pre-wrap">
                      {book.notes}
                    </p>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      </div>

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete "{book?.title}" from your library. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
};

export default BookDetail;
