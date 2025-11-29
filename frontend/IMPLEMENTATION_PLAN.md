# IntelliShelf Frontend v2 Implementation Plan

## 🎯 Project Status

**Base:** Frontend v2 prototype with UI shell, needs full backend integration and authentication.

---

## 🛠️ Tech Stack (v2)

### Core
- **React 18** - Stable production version
- **TypeScript** - Type safety
- **Vite 5** - Build tool with SWC (20x faster than Babel)
- **React Router 6** - Client-side routing

### UI & Styling
- **Radix UI** - Accessible headless component primitives (30+ components)
- **shadcn/ui** - Copy-paste component patterns built on Radix
- **Tailwind CSS** - Utility-first with HSL design token system
- **CVA (class-variance-authority)** - Type-safe component variants
- **Lucide React** - Icon library
- **simple-icons** - Brand logos (Google, GitHub, etc.)
- **tailwind-merge** - Intelligent className merging

### State & Data
- **TanStack Query v5** - Server state, caching, mutations, optimistic updates
- **React Hook Form** - Performant form handling
- **Zod** - Schema validation and type inference
- **Native Fetch API** - HTTP client (modern, built-in)

### Notifications & UX
- **Sonner** - Modern toast notifications
- **Radix Toast** - Accessible toast system
- **next-themes** - Theme management (dark mode ready)

---

## 🎨 Design System Architecture

### Design Token System (HSL)
```css
/* All colors use HSL with CSS custom properties */
:root {
  --background: 0 0% 6%;
  --foreground: 0 0% 98%;
  --primary: 186 100% 53%;
  --card: 0 0% 9%;
  --book-card: 0 0% 10%;
  --book-card-hover: 0 0% 12%;
}

/* Usage in Tailwind */
className="bg-card text-foreground hover:bg-book-card-hover"
```

**Benefits:**
- Semantic naming
- Easy theming (modify HSL values)
- Dark mode by default
- Component-specific tokens

### Component Patterns (shadcn + CVA)
```tsx
// Type-safe variants with CVA
const buttonVariants = cva("base-styles", {
  variants: {
    variant: { default, destructive, outline },
    size: { default, sm, lg, icon }
  }
});

// Composition with Slot API
<Button asChild>
  <Link to="/books">View Library</Link>
</Button>
```

---

## 📋 Implementation Phases

### **Phase 1: API Integration & Auth** 🔐

**Goal:** Connect to backend API, implement authentication flow

#### Infrastructure Setup
```typescript
// src/lib/api.ts - Fetch wrapper with TanStack Query
const API_URL = import.meta.env.VITE_API_BASE_URL;

// Helper to build query params (handles arrays, null/undefined)
const buildQueryParams = (params: Record<string, any>): URLSearchParams => {
  const searchParams = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    // Skip null/undefined
    if (value == null) return;

    // Handle arrays (e.g., tags, multiple filters)
    if (Array.isArray(value)) {
      value.forEach(v => searchParams.append(key, String(v)));
    } else {
      searchParams.append(key, String(value));
    }
  });

  return searchParams;
};

const api = {
  get: async <T>(endpoint: string, params?: Record<string, any>): Promise<T> => {
    let url = `${API_URL}${endpoint}`;

    if (params) {
      const queryParams = buildQueryParams(params);
      const queryString = queryParams.toString();
      if (queryString) {
        url += `?${queryString}`;
      }
    }

    const res = await fetch(url.toString(), {
      credentials: 'include', // Send cookies for refresh token
      headers: { 'Content-Type': 'application/json' }
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error);
    }

    return res.json();
  },

  post: async <T>(endpoint: string, body: any): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error);
    }

    return res.json();
  },

  put: async <T>(endpoint: string, body: any): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error);
    }

    return res.json();
  },

  delete: async <T>(endpoint: string): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'DELETE',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' }
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error);
    }

    return res.json();
  },

  upload: async <T>(endpoint: string, formData: FormData): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'POST',
      credentials: 'include',
      body: formData // No Content-Type header - browser sets it with boundary
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error);
    }

    return res.json();
  }
};

export default api;
```

#### Auth with TanStack Query
```typescript
// src/hooks/auth/useAuth.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import api from '@/lib/api';
import type { User, LoginDto, RegisterDto } from '@/types/auth';

export const useAuth = () => {
  const queryClient = useQueryClient();

  // Get current user (cached)
  const { data: user, isLoading } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: () => api.get<User>('/auth/me'),
    retry: false,
    staleTime: 5 * 60 * 1000 // 5 min
  });

  // Login mutation
  const login = useMutation({
    mutationFn: (data: LoginDto) => api.post<User>('/auth/login', data),
    onSuccess: (user) => {
      queryClient.setQueryData(['auth', 'me'], user);
      toast.success('Welcome back!');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Login failed');
    }
  });

  // Register mutation
  const register = useMutation({
    mutationFn: (data: RegisterDto) => api.post<User>('/auth/register', data),
    onSuccess: (user) => {
      queryClient.setQueryData(['auth', 'me'], user);
      toast.success('Account created successfully!');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Registration failed');
    }
  });

  // Logout mutation
  const logout = useMutation({
    mutationFn: () => api.post('/auth/logout', {}),
    onSuccess: () => {
      queryClient.setQueryData(['auth', 'me'], null);
      queryClient.clear(); // Clear all cached data
      toast.success('Logged out successfully');
    }
  });

  return {
    user,
    isLoading,
    isAuthenticated: !!user,
    login,
    register,
    logout
  };
};
```

#### Brand Icon Component
```tsx
// src/components/icons/GoogleIcon.tsx
import { siGoogle } from 'simple-icons';

interface GoogleIconProps {
  className?: string;
}

export const GoogleIcon = ({ className }: GoogleIconProps) => (
  <svg
    role="img"
    viewBox="0 0 24 24"
    className={className}
    fill="currentColor"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d={siGoogle.path} />
  </svg>
);
```

**Installation:**
```bash
npm install simple-icons
```

**Benefits:**
- Consistent with Lucide React pattern (used for other icons)
- Single source of truth for brand icons
- Auto-updated when simple-icons updates
- Reusable across components
- Type-safe

#### Single Auth Page (Mode Switching)
```tsx
// src/pages/Auth.tsx
import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { GoogleIcon } from '@/components/icons/GoogleIcon';
import LoginForm from '@/components/auth/LoginForm';
import RegisterForm from '@/components/auth/RegisterForm';
import { useAuth } from '@/hooks/auth/useAuth';

const Auth = () => {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const { isAuthenticated } = useAuth();

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleGoogleLogin = () => {
    const returnUrl = encodeURIComponent('/');
    window.location.href = `${import.meta.env.VITE_API_BASE_URL}/auth/google?returnUrl=${returnUrl}`;
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-background">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Welcome to intellishelf</CardTitle>
          <CardDescription>
            Manage your personal book library
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Tabs value={mode} onValueChange={(v) => setMode(v as 'login' | 'register')}>
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="login">Login</TabsTrigger>
              <TabsTrigger value="register">Register</TabsTrigger>
            </TabsList>

            <TabsContent value="login">
              <LoginForm />
            </TabsContent>

            <TabsContent value="register">
              <RegisterForm />
            </TabsContent>
          </Tabs>

          <div className="relative my-6">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-2 text-muted-foreground">Or continue with</span>
            </div>
          </div>

          <Button variant="outline" className="w-full" onClick={handleGoogleLogin}>
            <GoogleIcon className="mr-2 h-4 w-4" />
            Continue with Google
          </Button>
        </CardContent>
      </Card>
    </div>
  );
};

export default Auth;
```

#### Protected Routes
```tsx
// src/components/layout/ProtectedRoute.tsx
import { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '@/hooks/auth/useAuth';
import { Skeleton } from '@/components/ui/skeleton';

interface ProtectedRouteProps {
  children: ReactNode;
}

const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="space-y-4">
          <Skeleton className="h-12 w-64" />
          <Skeleton className="h-64 w-96" />
        </div>
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/auth" replace />;
  }

  return <>{children}</>;
};

export default ProtectedRoute;
```

**Deliverables:**
- [ ] Install `simple-icons` package for brand logos
- [ ] API client with fetch wrapper and query param builder (`src/lib/api.ts`)
- [ ] Auth types (`src/types/auth.ts`)
- [ ] Auth hooks with TanStack Query (`useAuth`, `useLogin`, `useRegister`)
- [ ] GoogleIcon component using simple-icons (`src/components/icons/GoogleIcon.tsx`)
- [ ] Single auth page with tab switching (`src/pages/Auth.tsx`)
- [ ] Login form component with validation
- [ ] Register form component with validation
- [ ] Google OAuth redirect flow
- [ ] Protected route wrapper (`src/components/layout/ProtectedRoute.tsx`)
- [ ] Error handling with Sonner toast notifications
- [ ] Update App.tsx routing to include `/auth` route
- [ ] Update protected routes to use `<ProtectedRoute>` wrapper

---

### **Phase 2: Books CRUD Operations** 📚

**Goal:** Full book management with backend integration

#### Books Query Hooks
```typescript
// src/hooks/books/useBooks.ts
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import type { PagedResult, Book, BooksQueryParams } from '@/types/book';

export const useBooks = (params: BooksQueryParams) => {
  return useQuery({
    queryKey: ['books', params],
    queryFn: () => api.get<PagedResult<Book>>('/books', params),
    staleTime: 2 * 60 * 1000 // 2 min
  });
};

// src/hooks/books/useBook.ts
export const useBook = (id: string) => {
  return useQuery({
    queryKey: ['books', id],
    queryFn: () => api.get<Book>(`/books/${id}`),
    enabled: !!id
  });
};
```

#### Books Mutation Hooks (Optimistic Updates)
```typescript
// src/hooks/books/useAddBook.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import api from '@/lib/api';
import type { Book, PagedResult } from '@/types/book';

export const useAddBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: FormData) => api.upload<Book>('/books', data),

    onMutate: async (newBookData) => {
      // Cancel outgoing queries
      await queryClient.cancelQueries({ queryKey: ['books'] });

      // Snapshot previous value
      const previousBooks = queryClient.getQueryData(['books']);

      return { previousBooks };
    },

    onSuccess: (newBook) => {
      // Invalidate to refetch with server data
      queryClient.invalidateQueries({ queryKey: ['books'] });

      toast.success('Book added successfully');
    },

    onError: (error: Error, _variables, context) => {
      // Rollback on error
      if (context?.previousBooks) {
        queryClient.setQueryData(['books'], context.previousBooks);
      }

      toast.error(`Failed to add book: ${error.message}`);
    }
  });
};

// src/hooks/books/useUpdateBook.ts
export const useUpdateBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: FormData }) =>
      api.upload<Book>(`/books/${id}`, data),

    onSuccess: (updatedBook) => {
      queryClient.invalidateQueries({ queryKey: ['books'] });
      queryClient.setQueryData(['books', updatedBook.id], updatedBook);

      toast.success('Book updated successfully');
    },

    onError: (error: Error) => {
      toast.error(`Failed to update book: ${error.message}`);
    }
  });
};

// src/hooks/books/useDeleteBook.ts
export const useDeleteBook = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => api.delete(`/books/${id}`),

    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['books'] });

      toast.success('Book deleted successfully');
    },

    onError: (error: Error) => {
      toast.error(`Failed to delete book: ${error.message}`);
    }
  });
};
```

#### Image Upload Component
```tsx
// src/components/books/ImageUpload.tsx
import { useState, useRef } from 'react';
import { Upload, X } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface ImageUploadProps {
  value?: File | string;
  onChange: (file: File | null) => void;
}

const ImageUpload = ({ value, onChange }: ImageUploadProps) => {
  const [preview, setPreview] = useState<string | null>(
    typeof value === 'string' ? value : null
  );
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (file: File | null) => {
    if (!file) {
      setPreview(null);
      onChange(null);
      return;
    }

    if (file.type.startsWith('image/')) {
      onChange(file);
      const reader = new FileReader();
      reader.onloadend = () => setPreview(reader.result as string);
      reader.readAsDataURL(file);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const file = e.dataTransfer.files[0];
    handleFileChange(file);
  };

  const handleClick = () => {
    inputRef.current?.click();
  };

  const handleClear = () => {
    handleFileChange(null);
    if (inputRef.current) {
      inputRef.current.value = '';
    }
  };

  return (
    <div className="space-y-2">
      <div
        onDrop={handleDrop}
        onDragOver={(e) => e.preventDefault()}
        onClick={handleClick}
        className="border-2 border-dashed border-border rounded-lg p-8 text-center cursor-pointer hover:border-primary transition-colors relative"
      >
        {preview ? (
          <>
            <img
              src={preview}
              alt="Preview"
              className="max-h-64 mx-auto rounded-lg"
            />
            <Button
              type="button"
              variant="destructive"
              size="icon"
              className="absolute top-2 right-2"
              onClick={(e) => {
                e.stopPropagation();
                handleClear();
              }}
            >
              <X className="w-4 h-4" />
            </Button>
          </>
        ) : (
          <div>
            <Upload className="w-12 h-12 mx-auto mb-2 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              Drag & drop or click to upload
            </p>
            <p className="text-xs text-muted-foreground mt-1">
              PNG, JPG up to 10MB
            </p>
          </div>
        )}
      </div>

      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        className="hidden"
        onChange={(e) => handleFileChange(e.target.files?.[0] || null)}
      />
    </div>
  );
};

export default ImageUpload;
```

#### Book Form with React Hook Form + Zod
```typescript
// src/lib/schemas/bookSchema.ts
import { z } from 'zod';

export const bookSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  authors: z.string().min(1, 'Author is required'),
  description: z.string().optional(),
  isbn: z.string().optional(),
  publisher: z.string().optional(),
  publishedDate: z.string().optional(),
  pageCount: z.coerce.number().optional(),
  tags: z.string().optional(),
  imageFile: z.instanceof(File).optional()
});

export type BookFormData = z.infer<typeof bookSchema>;

// src/components/books/BookForm.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import ImageUpload from './ImageUpload';
import { bookSchema, type BookFormData } from '@/lib/schemas/bookSchema';
import { useAddBook } from '@/hooks/books/useAddBook';
import type { Book } from '@/types/book';

interface BookFormProps {
  book?: Book;
  onSuccess?: () => void;
}

const BookForm = ({ book, onSuccess }: BookFormProps) => {
  const { mutate: addBook, isPending } = useAddBook();

  const form = useForm<BookFormData>({
    resolver: zodResolver(bookSchema),
    defaultValues: book ? {
      title: book.title,
      authors: book.authors,
      description: book.description,
      isbn: book.isbn,
      publisher: book.publisher,
      publishedDate: book.publishedDate,
      pageCount: book.pageCount,
      tags: book.tags?.join(', ')
    } : {}
  });

  const onSubmit = (data: BookFormData) => {
    const formData = new FormData();

    Object.entries(data).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        if (key === 'imageFile' && value instanceof File) {
          formData.append('imageFile', value);
        } else if (key !== 'imageFile') {
          formData.append(key, String(value));
        }
      }
    });

    addBook(formData, {
      onSuccess: () => {
        form.reset();
        onSuccess?.();
      }
    });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="imageFile"
          render={({ field: { value, onChange, ...field } }) => (
            <FormItem>
              <FormLabel>Cover Image</FormLabel>
              <FormControl>
                <ImageUpload
                  value={value}
                  onChange={onChange}
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="title"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Title *</FormLabel>
              <FormControl>
                <Input placeholder="The Great Gatsby" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="authors"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Author(s) *</FormLabel>
              <FormControl>
                <Input placeholder="F. Scott Fitzgerald" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Description</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="A brief description..."
                  className="resize-none"
                  rows={4}
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="isbn"
            render={({ field }) => (
              <FormItem>
                <FormLabel>ISBN</FormLabel>
                <FormControl>
                  <Input placeholder="978-3-16-148410-0" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="pageCount"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Pages</FormLabel>
                <FormControl>
                  <Input type="number" placeholder="320" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="publisher"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Publisher</FormLabel>
                <FormControl>
                  <Input placeholder="Scribner" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="publishedDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Published Date</FormLabel>
                <FormControl>
                  <Input type="date" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="tags"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Tags</FormLabel>
              <FormControl>
                <Input placeholder="fiction, classic, american (comma separated)" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" disabled={isPending} className="w-full">
          {isPending ? 'Adding...' : book ? 'Update Book' : 'Add Book'}
        </Button>
      </form>
    </Form>
  );
};

export default BookForm;
```

#### Enhanced BookCard with Actions
```tsx
// Update src/components/BookCard.tsx
import { useState } from 'react';
import { Edit, Trash } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
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
import { useDeleteBook } from '@/hooks/books/useDeleteBook';
import type { Book } from '@/types/book';

interface BookCardProps {
  book: Book;
  onClick?: () => void;
}

const BookCard = ({ book, onClick }: BookCardProps) => {
  const navigate = useNavigate();
  const { mutate: deleteBook } = useDeleteBook();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else {
      navigate(`/book/${book.id}`);
    }
  };

  const handleEdit = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigate(`/edit/${book.id}`);
  };

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    setShowDeleteDialog(true);
  };

  const confirmDelete = () => {
    deleteBook(book.id);
    setShowDeleteDialog(false);
  };

  return (
    <>
      <Card
        className="bg-book-card hover:bg-book-card-hover border-border transition-all cursor-pointer group overflow-hidden relative"
        onClick={handleClick}
      >
        <div className="aspect-[2/3] relative overflow-hidden bg-secondary">
          {book.coverImageUrl ? (
            <img
              src={book.coverImageUrl}
              alt={book.title}
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center">
              <span className="text-muted-foreground text-sm">No cover</span>
            </div>
          )}

          {/* Hover overlay with actions */}
          <div className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
            <Button size="icon" variant="secondary" onClick={handleEdit}>
              <Edit className="w-4 h-4" />
            </Button>
            <Button size="icon" variant="destructive" onClick={handleDelete}>
              <Trash className="w-4 h-4" />
            </Button>
          </div>
        </div>

        <div className="p-4">
          <h3 className="font-semibold text-foreground line-clamp-2 mb-1">
            {book.title}
          </h3>
          <p className="text-sm text-muted-foreground line-clamp-1">
            {book.authors}
          </p>
        </div>
      </Card>

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
```

**Deliverables:**
- [ ] Books query hooks (`useBooks`, `useBook`)
- [ ] Books mutation hooks with optimistic updates (`useAddBook`, `useUpdateBook`, `useDeleteBook`)
- [ ] Book types (`src/types/book.ts`)
- [ ] Image upload component with drag-and-drop (`src/components/books/ImageUpload.tsx`)
- [ ] Book form with validation (`src/components/books/BookForm.tsx`)
- [ ] Book schema (`src/lib/schemas/bookSchema.ts`)
- [ ] Update BookCard with edit/delete actions
- [ ] Delete confirmation AlertDialog
- [ ] Status badge component (`src/components/books/StatusBadge.tsx`)
- [ ] Loading skeletons for books grid
- [ ] Empty state component when no books
- [ ] Update Library.tsx page to use real API data
- [ ] Update AddBooks.tsx page with BookForm
- [ ] Update BookDetail.tsx page with real API data

---

### **Phase 3: Search & Filtering** 🔍

**Goal:** Implement search with debouncing and status filtering

#### Search Hooks
```typescript
// src/hooks/utils/useDebounce.ts
import { useState, useEffect } from 'react';

export function useDebounce<T>(value: T, delay: number = 300): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

// src/hooks/books/useSearchBooks.ts
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useDebounce } from '@/hooks/utils/useDebounce';
import api from '@/lib/api';
import type { PagedResult, Book } from '@/types/book';

export const useSearchBooks = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [status, setStatus] = useState<number | null>(null);

  // Debounce search term
  const debouncedSearch = useDebounce(searchTerm, 300);

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['books', 'search', debouncedSearch, status],
    queryFn: () => api.get<PagedResult<Book>>('/books/search', {
      searchTerm: debouncedSearch,
      ...(status !== null && { status })
    }),
    enabled: debouncedSearch.length > 0 || status !== null
  });

  return {
    data,
    isLoading: isLoading || isFetching,
    searchTerm,
    setSearchTerm,
    status,
    setStatus,
    hasActiveFilters: debouncedSearch.length > 0 || status !== null
  };
};
```

#### Enhanced Search Bar
```tsx
// Update src/components/SearchBar.tsx
import { Search, X } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

const SearchBar = ({
  value,
  onChange,
  placeholder = 'Search by title, author, ISBN...'
}: SearchBarProps) => {
  return (
    <div className="relative">
      <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
      <Input
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="pl-10 pr-10"
      />
      {value && (
        <Button
          variant="ghost"
          size="icon"
          className="absolute right-1 top-1/2 -translate-y-1/2 h-8 w-8"
          onClick={() => onChange('')}
        >
          <X className="w-4 h-4" />
        </Button>
      )}
    </div>
  );
};

export default SearchBar;
```

#### Status Filter Component
```tsx
// src/components/books/StatusFilter.tsx
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';

interface StatusFilterProps {
  value: number | null;
  onChange: (value: number | null) => void;
}

const StatusFilter = ({ value, onChange }: StatusFilterProps) => {
  return (
    <Tabs
      value={value?.toString() ?? 'all'}
      onValueChange={(v) => onChange(v === 'all' ? null : Number(v))}
    >
      <TabsList>
        <TabsTrigger value="all">All</TabsTrigger>
        <TabsTrigger value="0">Unread</TabsTrigger>
        <TabsTrigger value="1">Reading</TabsTrigger>
        <TabsTrigger value="2">Read</TabsTrigger>
      </TabsList>
    </Tabs>
  );
};

export default StatusFilter;
```

#### Updated Library Page with Search
```tsx
// Update src/pages/Library.tsx
import { useState } from 'react';
import SearchBar from '@/components/SearchBar';
import StatusFilter from '@/components/books/StatusFilter';
import BookCard from '@/components/BookCard';
import { Skeleton } from '@/components/ui/skeleton';
import { useBooks } from '@/hooks/books/useBooks';
import { useSearchBooks } from '@/hooks/books/useSearchBooks';

const Library = () => {
  const {
    searchTerm,
    setSearchTerm,
    status,
    setStatus,
    data: searchData,
    isLoading: isSearching,
    hasActiveFilters
  } = useSearchBooks();

  const { data: allBooks, isLoading: isLoadingAll } = useBooks({
    page: 1,
    pageSize: 50
  });

  const books = hasActiveFilters ? searchData?.items : allBooks?.items;
  const isLoading = hasActiveFilters ? isSearching : isLoadingAll;

  return (
    <div className="p-6 space-y-6">
      <div className="space-y-4">
        <h1 className="text-3xl font-bold">Your Library</h1>

        <div className="flex gap-4 items-center">
          <div className="flex-1">
            <SearchBar value={searchTerm} onChange={setSearchTerm} />
          </div>
          <StatusFilter value={status} onChange={setStatus} />
        </div>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {Array.from({ length: 12 }).map((_, i) => (
            <Skeleton key={i} className="aspect-[2/3]" />
          ))}
        </div>
      ) : books && books.length > 0 ? (
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {books.map((book) => (
            <BookCard key={book.id} book={book} />
          ))}
        </div>
      ) : (
        <div className="text-center py-12">
          <p className="text-muted-foreground">
            {hasActiveFilters ? 'No books found matching your search' : 'No books in your library yet'}
          </p>
        </div>
      )}
    </div>
  );
};

export default Library;
```

**Deliverables:**
- [ ] Debounce hook (`useDebounce`)
- [ ] Search books hook (`useSearchBooks`)
- [ ] Enhanced SearchBar with clear button
- [ ] Status filter component
- [ ] Update Library page with search and filter
- [ ] Empty state for no search results
- [ ] Loading states during search

---

### **Phase 4: Polish & Production Ready** ✨

**Goal:** Accessibility, responsive design, error handling

#### Responsive Layout
```tsx
// Update src/components/Layout.tsx for mobile
import { useState } from 'react';
import { ReactNode } from 'react';
import { Library, Plus, MessageSquare, Settings, BookOpen, Menu, LogOut } from 'lucide-react';
import { NavLink } from '@/components/NavLink';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { useAuth } from '@/hooks/auth/useAuth';
import { useMobile } from '@/hooks/use-mobile';

interface LayoutProps {
  children: ReactNode;
}

const navItems = [
  { title: 'Library', icon: Library, path: '/' },
  { title: 'Add Books', icon: Plus, path: '/add' },
  { title: 'AI Chat', icon: MessageSquare, path: '/chat' },
  { title: 'Settings', icon: Settings, path: '/settings' },
];

const Layout = ({ children }: LayoutProps) => {
  const { user, logout } = useAuth();
  const isMobile = useMobile();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const handleLogout = () => {
    logout.mutate();
  };

  const SidebarContent = () => (
    <>
      <div className="p-6 border-b border-sidebar-border">
        <div className="flex items-center gap-2">
          <BookOpen className="w-8 h-8 text-primary" />
          <h1 className="text-2xl font-bold text-foreground">intellishelf</h1>
        </div>
      </div>

      <nav className="flex-1 p-4">
        <ul className="space-y-2">
          {navItems.map((item) => (
            <li key={item.path}>
              <NavLink
                to={item.path}
                end
                className="flex items-center gap-3 px-4 py-3 rounded-lg text-sidebar-foreground hover:bg-nav-hover transition-colors"
                activeClassName="bg-sidebar-accent text-sidebar-accent-foreground"
                onClick={() => isMobile && setSidebarOpen(false)}
              >
                <item.icon className="w-5 h-5" />
                <span className="font-medium">{item.title}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      <div className="p-4 border-t border-sidebar-border">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="w-full justify-start gap-3 px-4">
              <Avatar className="w-8 h-8">
                <AvatarFallback>
                  {user?.email?.[0].toUpperCase() || 'U'}
                </AvatarFallback>
              </Avatar>
              <div className="flex-1 text-left text-sm">
                <p className="font-medium truncate">{user?.email}</p>
              </div>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuLabel>My Account</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={handleLogout}>
              <LogOut className="w-4 h-4 mr-2" />
              Logout
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </>
  );

  return (
    <div className="flex h-screen w-full bg-background">
      {isMobile ? (
        <>
          {/* Mobile Header */}
          <div className="fixed top-0 left-0 right-0 h-16 bg-sidebar border-b border-sidebar-border flex items-center justify-between px-4 z-50">
            <Sheet open={sidebarOpen} onOpenChange={setSidebarOpen}>
              <SheetTrigger asChild>
                <Button variant="ghost" size="icon">
                  <Menu className="w-6 h-6" />
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="w-64 p-0 bg-sidebar">
                <div className="flex flex-col h-full">
                  <SidebarContent />
                </div>
              </SheetContent>
            </Sheet>

            <div className="flex items-center gap-2">
              <BookOpen className="w-6 h-6 text-primary" />
              <h1 className="text-xl font-bold">intellishelf</h1>
            </div>

            <div className="w-10" /> {/* Spacer for centering */}
          </div>

          {/* Main Content with top padding */}
          <main className="flex-1 overflow-auto pt-16">
            {children}
          </main>
        </>
      ) : (
        <>
          {/* Desktop Sidebar */}
          <aside className="w-64 bg-sidebar border-r border-sidebar-border flex flex-col">
            <SidebarContent />
          </aside>

          {/* Main Content */}
          <main className="flex-1 overflow-auto">
            {children}
          </main>
        </>
      )}
    </div>
  );
};

export default Layout;
```

#### Error Boundary
```tsx
// src/components/ErrorBoundary.tsx
import React, { Component, ReactNode } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { AlertTriangle } from 'lucide-react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex items-center justify-center min-h-screen bg-background p-4">
          <Card className="max-w-md w-full">
            <CardHeader>
              <div className="flex items-center gap-2">
                <AlertTriangle className="w-6 h-6 text-destructive" />
                <CardTitle>Something went wrong</CardTitle>
              </div>
              <CardDescription>
                An unexpected error occurred. Please try refreshing the page.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {this.state.error && (
                <div className="p-3 bg-muted rounded-md">
                  <p className="text-sm font-mono text-muted-foreground">
                    {this.state.error.message}
                  </p>
                </div>
              )}
              <Button
                onClick={() => window.location.reload()}
                className="w-full"
              >
                Reload Page
              </Button>
            </CardContent>
          </Card>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
```

#### Update App.tsx with Error Boundary
```tsx
// Update src/App.tsx
import { Toaster } from '@/components/ui/toaster';
import { Toaster as Sonner } from '@/components/ui/sonner';
import { TooltipProvider } from '@/components/ui/tooltip';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import ErrorBoundary from './components/ErrorBoundary';
import ProtectedRoute from './components/layout/ProtectedRoute';
import Layout from './components/Layout';
import Index from './pages/Index';
import AddBooks from './pages/AddBooks';
import Chat from './pages/Chat';
import Settings from './pages/Settings';
import BookDetail from './pages/BookDetail';
import Auth from './pages/Auth';
import NotFound from './pages/NotFound';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

const App = () => (
  <ErrorBoundary>
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>
        <Toaster />
        <Sonner />
        <BrowserRouter>
          <Routes>
            <Route path="/auth" element={<Auth />} />
            <Route
              path="/*"
              element={
                <ProtectedRoute>
                  <Layout>
                    <Routes>
                      <Route path="/" element={<Index />} />
                      <Route path="/add" element={<AddBooks />} />
                      <Route path="/chat" element={<Chat />} />
                      <Route path="/settings" element={<Settings />} />
                      <Route path="/book/:id" element={<BookDetail />} />
                      <Route path="*" element={<NotFound />} />
                    </Routes>
                  </Layout>
                </ProtectedRoute>
              }
            />
          </Routes>
        </BrowserRouter>
      </TooltipProvider>
    </QueryClientProvider>
  </ErrorBoundary>
);

export default App;
```

**Deliverables:**
- [ ] Mobile responsive layout with Sheet sidebar
- [ ] User menu with logout in sidebar
- [ ] Error boundary component
- [ ] Update App.tsx with error boundary and protected routes
- [ ] Loading skeletons for all data fetching
- [ ] Toast notifications for all actions (already using Sonner)
- [ ] Keyboard navigation support (Tab, Enter, Esc)
- [ ] Accessibility audit (WCAG AA)

---

## 📁 Final Project Structure

```
frontend-v2/
├── src/
│   ├── lib/
│   │   ├── api.ts                # Fetch wrapper utilities
│   │   ├── utils.ts              # cn() helper (exists)
│   │   └── schemas/
│   │       ├── bookSchema.ts     # Zod schemas for books
│   │       └── authSchema.ts     # Zod schemas for auth
│   │
│   ├── hooks/
│   │   ├── auth/
│   │   │   ├── useAuth.ts        # Main auth hook
│   │   │   ├── useLogin.ts       # Login mutation
│   │   │   └── useRegister.ts    # Register mutation
│   │   │
│   │   ├── books/
│   │   │   ├── useBooks.ts       # List books (paginated)
│   │   │   ├── useBook.ts        # Single book
│   │   │   ├── useAddBook.ts     # Add mutation
│   │   │   ├── useUpdateBook.ts  # Update mutation
│   │   │   ├── useDeleteBook.ts  # Delete mutation
│   │   │   └── useSearchBooks.ts # Search with filters
│   │   │
│   │   └── utils/
│   │       ├── useDebounce.ts    # Debounce hook
│   │       ├── use-mobile.tsx    # Mobile detection (exists)
│   │       └── use-toast.ts      # Toast hook (exists)
│   │
│   ├── components/
│   │   ├── ui/                   # 30+ shadcn components (exists)
│   │   │
│   │   ├── icons/
│   │   │   └── GoogleIcon.tsx    # Brand icons using simple-icons
│   │   │
│   │   ├── auth/
│   │   │   ├── LoginForm.tsx
│   │   │   └── RegisterForm.tsx
│   │   │
│   │   ├── books/
│   │   │   ├── BookForm.tsx
│   │   │   ├── ImageUpload.tsx
│   │   │   ├── StatusBadge.tsx
│   │   │   ├── StatusFilter.tsx
│   │   │   └── EmptyState.tsx
│   │   │
│   │   ├── layout/
│   │   │   └── ProtectedRoute.tsx
│   │   │
│   │   ├── BookCard.tsx          # Update existing
│   │   ├── Layout.tsx            # Update for mobile + user menu
│   │   ├── NavLink.tsx           # Exists
│   │   ├── SearchBar.tsx         # Update existing
│   │   └── ErrorBoundary.tsx
│   │
│   ├── pages/
│   │   ├── Auth.tsx              # NEW: Single auth page
│   │   ├── Index.tsx             # Wrapper for Library
│   │   ├── Library.tsx           # Update with API
│   │   ├── AddBooks.tsx          # Update with form
│   │   ├── BookDetail.tsx        # Update with API
│   │   ├── Chat.tsx              # Keep for later
│   │   ├── Settings.tsx          # Keep for later
│   │   └── NotFound.tsx          # Exists
│   │
│   ├── types/
│   │   ├── auth.ts               # User, LoginDto, RegisterDto
│   │   ├── book.ts               # Book, PagedResult, QueryParams
│   │   └── api.ts                # Common API types
│   │
│   ├── App.tsx                   # Update with error boundary
│   ├── main.tsx
│   └── index.css
│
├── .env.local                    # VITE_API_BASE_URL
└── package.json
```

---

## 🎯 Key Patterns & Best Practices

### 1. TanStack Query as Single Source of Truth
- **No separate global state** for server data
- Cache keys follow convention: `['resource', ...params]`
- Optimistic updates with rollback on error
- Invalidation after mutations

### 2. shadcn/ui Component Composition
```tsx
// Prefer composition over props
<Dialog>
  <DialogTrigger asChild>
    <Button>Add Book</Button>
  </DialogTrigger>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>Add New Book</DialogTitle>
    </DialogHeader>
    <BookForm />
  </DialogContent>
</Dialog>
```

### 3. Form Handling Pattern
```tsx
// Always: React Hook Form + Zod + TanStack Query mutation
const form = useForm({ resolver: zodResolver(schema) });
const { mutate, isPending } = useAddBook();

<form onSubmit={form.handleSubmit((data) => mutate(data))}>
  <FormField name="title" control={form.control} render={...} />
  <Button type="submit" disabled={isPending}>
    {isPending ? <Spinner /> : 'Submit'}
  </Button>
</form>
```

### 4. Error Handling Strategy
- **API errors:** Caught in mutation `onError`, shown as toast
- **Form errors:** Displayed inline with React Hook Form
- **Network errors:** Global error boundary
- **401 errors:** Clear auth cache, redirect to login

### 5. Loading States
- **Query loading:** Show skeletons
- **Mutation loading:** Disable button, show spinner
- **Optimistic updates:** Immediate UI feedback, rollback on error

---

## ⚙️ Environment Setup

### `.env.local`
```bash
VITE_API_BASE_URL=http://localhost:5000
```

### Development Commands
```bash
# Install dependencies
npm install

# Start dev server (port 3000)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint
npm run lint
```

---

## ✅ Phase Completion Criteria

### Phase 1 (Auth) ✓
- User can register with email/password
- User can login with email/password
- User can login with Google OAuth
- Auth state persists (cookie-based)
- Protected routes work
- Logout clears all cached data

### Phase 2 (CRUD) ✓
- User can view all books (paginated)
- User can add book with image upload
- User can edit book
- User can delete book (with confirmation)
- User can change reading status
- Optimistic updates work with rollback

### Phase 3 (Search) ✓
- User can search books (debounced)
- User can filter by status
- Search + filter work together
- Empty state shown for no results

### Phase 4 (Polish) ✓
- Mobile responsive (Sheet sidebar)
- All actions show toast feedback
- Loading states everywhere
- Keyboard navigation works
- Error boundary handles crashes
- User menu with logout

---

## 🔑 Critical Technical Decisions

**TanStack Query over Zustand/Redux:**
- Server state belongs in cache, not global state
- Automatic refetching, caching, deduplication
- Optimistic updates with rollback
- Simpler mental model

**Native Fetch over Axios:**
- Modern, built-in, smaller bundle
- Works great with TanStack Query
- TypeScript-friendly
- No interceptor complexity needed (cookies handle auth)

**shadcn/ui over Component Library:**
- You own the code (copy-paste)
- Full customization control
- Radix UI handles accessibility
- Active community, modern patterns

**Cookie-based Auth over JWT localStorage:**
- HttpOnly cookies prevent XSS
- Backend handles refresh automatically
- No token expiry logic in frontend
- Simpler security model

---

## 🚀 API Integration Notes

### Backend Endpoints (from AGENTS.md)

**Auth:**
- `POST /auth/register` - Email/password registration
- `POST /auth/login` - Email/password login
- `GET /auth/google` - Google OAuth redirect
- `GET /auth/me` - Get current user
- `POST /auth/logout` - Logout (clears cookie)

**Books:**
- `GET /books` - List books (paginated, with search/filter)
- `GET /books/{id}` - Get single book
- `POST /books` - Add book (multipart/form-data for image)
- `PUT /books/{id}` - Update book
- `DELETE /books/{id}` - Delete book
- `POST /books/parse-text` - AI parsing (future)

### Response Formats

**Success (TryResult):**
```json
{
  "isSuccess": true,
  "value": { ... }
}
```

**Error (TryResult):**
```json
{
  "isSuccess": false,
  "error": {
    "code": "BookNotFound",
    "message": "Book not found."
  }
}
```

**Paged Result:**
```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 25
}
```

---

**Last Updated:** 2025-11-16
**Version:** 2.0 (v2-based)
**Current Status:** Prototype → Full Implementation Ready
