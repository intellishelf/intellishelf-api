import { useState } from "react";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { useToast } from "@/components/ui/use-toast";
import { BookPlus } from "lucide-react";

const AddBooks = () => {
  const { toast } = useToast();
  const [formData, setFormData] = useState({
    title: "",
    author: "",
    year: "",
    isbn: "",
    notes: "",
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    toast({
      title: "Book added!",
      description: `"${formData.title}" has been added to your library.`,
    });
    setFormData({
      title: "",
      author: "",
      year: "",
      isbn: "",
      notes: "",
    });
  };

  return (
    <div className="h-full overflow-auto">
      <div className="max-w-2xl mx-auto p-6">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-foreground mb-2">Add Books</h1>
          <p className="text-muted-foreground">
            Add new books to your digital library
          </p>
        </div>

        <Card className="bg-card border-border p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="title">Title *</Label>
              <Input
                id="title"
                value={formData.title}
                onChange={(e) =>
                  setFormData({ ...formData, title: e.target.value })
                }
                required
                placeholder="Enter book title"
                className="bg-secondary border-border"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="author">Author *</Label>
              <Input
                id="author"
                value={formData.author}
                onChange={(e) =>
                  setFormData({ ...formData, author: e.target.value })
                }
                required
                placeholder="Enter author name"
                className="bg-secondary border-border"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="year">Year</Label>
                <Input
                  id="year"
                  type="number"
                  value={formData.year}
                  onChange={(e) =>
                    setFormData({ ...formData, year: e.target.value })
                  }
                  placeholder="2024"
                  className="bg-secondary border-border"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="isbn">ISBN</Label>
                <Input
                  id="isbn"
                  value={formData.isbn}
                  onChange={(e) =>
                    setFormData({ ...formData, isbn: e.target.value })
                  }
                  placeholder="978-0-123456-78-9"
                  className="bg-secondary border-border"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="notes">Notes</Label>
              <Textarea
                id="notes"
                value={formData.notes}
                onChange={(e) =>
                  setFormData({ ...formData, notes: e.target.value })
                }
                placeholder="Add any notes about this book..."
                className="bg-secondary border-border min-h-24"
              />
            </div>

            <Button type="submit" className="w-full">
              <BookPlus className="w-4 h-4 mr-2" />
              Add Book
            </Button>
          </form>
        </Card>
      </div>
    </div>
  );
};

export default AddBooks;
