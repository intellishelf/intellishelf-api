# IntelliShelf Frontend Implementation Plan

## 📊 Project Overview

A modern, minimalistic frontend for the IntelliShelf book management application. Built with React, TypeScript, and TailwindCSS for a clean, performant user experience.

---

## 🛠️ Tech Stack

### Core Technologies
- **React 18** - Modern UI library with concurrent features
- **TypeScript** - Type-safe development
- **Vite** - Next-generation build tool (fast HMR, optimized builds)
- **React Router v6** - Declarative routing

### Styling & UI
- **TailwindCSS** - Utility-first CSS framework
- **Headless UI** - Unstyled, accessible components
- **Lucide React** - Beautiful, consistent icon set

### State Management
- **TanStack Query (React Query)** - Server state management, caching, mutations
- **Zustand** - Lightweight global state (auth, UI preferences)

### Forms & Validation
- **React Hook Form** - Performant form handling
- **Zod** - Schema validation and type inference

### HTTP Client
- **Axios** - Promise-based HTTP client with interceptors

---

## 🎨 Design Philosophy

### Visual Design Principles
- **Minimalistic** - Clean layouts with generous whitespace
- **Modern** - Subtle shadows, rounded corners, smooth transitions
- **Accessible** - WCAG 2.1 AA compliance, keyboard navigation
- **Responsive** - Mobile-first approach, works on all screen sizes

### Color Palette
```
Primary:   Indigo (#4F46E5)  - CTAs, active states, links
Neutral:   Gray (#F9FAFB → #111827) - Text, backgrounds, borders
Success:   Green (#10B981)   - Success messages, completed states
Error:     Red (#EF4444)     - Error messages, destructive actions
Warning:   Amber (#F59E0B)   - Warnings, pending states
```

### Typography
- Font Family: Inter (clean, modern sans-serif)
- Scale: Tailwind's default type scale
- Hierarchy: Clear distinction between headings, body, and captions

---

## 📋 Implementation Phases

### **Phase 1: Foundation & Authentication** 🔐
**Goal:** Setup project infrastructure and complete authentication flow

**Estimated Time:** 6-8 hours

#### Deliverables
- [x] Vite + React + TypeScript project initialization
- [x] TailwindCSS configuration with custom theme
- [x] Project folder structure
- [x] API client service (axios + interceptors)
- [x] Auth store with Zustand (token management, auto-refresh)
- [x] Route protection (PublicRoute, ProtectedRoute)
- [x] Reusable UI components (Button, Input, Card, Modal)
- [x] Login page (email + Google OAuth redirect)
- [x] Registration page
- [x] Basic layout shell (Header, Navigation)

#### Key Features
- **Token Management:** JWT storage, auto-refresh on 401, logout on invalid token
- **Google OAuth:** Redirect flow to `/auth/google?returnUrl=...`
- **Form Validation:** Real-time validation with error messages
- **Loading States:** Spinners, disabled buttons during async operations
- **Error Handling:** User-friendly error messages

#### Technical Details
```typescript
// Auth Store Structure
interface AuthState {
  user: User | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
  setUser: (user: User | null) => void;
}

// API Client Features
- Base URL configuration via environment variables
- Request interceptors: Attach JWT token to all requests
- Response interceptors: Auto-refresh on 401, handle errors globally
- TypeScript types for all endpoints
```

---

### **Phase 2: Books CRUD & Core UI** 📚
**Goal:** Complete book management functionality

**Estimated Time:** 8-10 hours

#### Deliverables
- [ ] Books list view (paginated grid with responsive columns)
- [ ] Book card component (cover, title, author, status badge)
- [ ] Add book modal/page with image upload (drag-and-drop)
- [ ] Edit book modal/page (pre-filled form)
- [ ] Delete confirmation modal
- [ ] Book detail view (full information display)
- [ ] Reading status management (Unread/Reading/Read with date tracking)
- [ ] Empty states (no books yet, clear illustrations/messages)
- [ ] Loading skeletons (shimmer effect during data fetch)

#### Key Features
- **Image Upload:** Drag-and-drop or click to upload, preview before submit
- **Optimistic Updates:** UI updates immediately, rollback on error
- **Pagination:** Page controls, page size selector (25/50/100)
- **Sorting:** By title, author, date added, publication date (ascending/descending)
- **Status Badges:** Color-coded pills (gray=Unread, blue=Reading, green=Read)
- **Infinite Scroll:** (Optional enhancement) Load more on scroll

#### Component Hierarchy
```
DashboardPage
├── BooksHeader (search, filters, add button)
├── BooksToolbar (sort, view toggle, pagination info)
├── BooksGrid
│   └── BookCard (repeating)
│       ├── BookCover (image with fallback)
│       ├── BookInfo (title, author)
│       ├── StatusBadge
│       └── ActionMenu (edit, delete, view)
└── Pagination

BookDetailPage
├── BookCover (large)
├── BookMetadata (all fields)
├── ReadingProgress (status, dates)
└── ActionButtons (edit, delete, back)

AddBookModal / EditBookModal
├── ImageUploadZone
└── BookForm
    ├── TextInputs (title, author, ISBN, etc.)
    ├── TextArea (description)
    ├── DatePickers (publication, reading dates)
    ├── TagInput (multi-value)
    └── StatusSelect (dropdown)
```

#### Data Flow (React Query)
```typescript
// Books queries
useBooks({ page, pageSize, orderBy, ascending })
useBook(bookId)

// Books mutations
useAddBook()
useUpdateBook()
useDeleteBook()

// Optimistic updates example
const { mutate: deleteBook } = useDeleteBook({
  onMutate: async (bookId) => {
    // Cancel outgoing refetches
    await queryClient.cancelQueries(['books'])
    // Snapshot previous value
    const previous = queryClient.getQueryData(['books'])
    // Optimistically update
    queryClient.setQueryData(['books'], (old) =>
      old.items.filter(book => book.id !== bookId)
    )
    return { previous }
  },
  onError: (err, bookId, context) => {
    // Rollback on error
    queryClient.setQueryData(['books'], context.previous)
  },
  onSettled: () => {
    // Refetch after success or error
    queryClient.invalidateQueries(['books'])
  }
})
```

---

### **Phase 3: Search & Filtering** 🔍
**Goal:** Implement powerful search and filtering capabilities

**Estimated Time:** 3-4 hours

#### Deliverables
- [ ] Search bar component with debounced input (300ms)
- [ ] Status filter tabs (All, Unread, Reading, Read)
- [ ] Search results highlighting (bold matching terms)
- [ ] "No results" empty state with search suggestions
- [ ] Clear filters button (reset to default view)
- [ ] Search history (localStorage, recent searches)

#### Key Features
- **Debounced Search:** Prevent excessive API calls, wait 300ms after typing stops
- **Combined Filters:** Search term + status filter work together
- **Full-Text Search:** Backend searches across title, authors, description, publisher, tags, ISBN
- **Keyboard Shortcuts:** "/" to focus search bar, "Esc" to clear
- **Search Persistence:** Preserve search state in URL query params (shareable links)

#### Implementation Details
```typescript
// Custom hook for debounced search
const useDebounce = (value: string, delay: number) => {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const handler = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(handler);
  }, [value, delay]);

  return debouncedValue;
};

// Search component usage
const [searchTerm, setSearchTerm] = useState('');
const debouncedSearch = useDebounce(searchTerm, 300);
const { data } = useSearchBooks({
  searchTerm: debouncedSearch,
  status: selectedStatus
});
```

#### URL State Management
```
/dashboard?search=gatsby&status=2&page=1&sort=title&order=asc

- Preserves filters on page refresh
- Shareable search results
- Browser back/forward navigation
```

---

### **Phase 4: Polish & UX Enhancements** ✨
**Goal:** Production-ready polish and accessibility

**Estimated Time:** 4-5 hours

#### Deliverables
- [ ] Toast notification system (success/error/info)
- [ ] Loading states and skeleton screens
- [ ] Smooth transitions and animations (framer-motion)
- [ ] Responsive mobile design (hamburger menu, bottom nav)
- [ ] Accessibility audit (ARIA labels, keyboard navigation, focus management)
- [ ] Dark mode support (toggle in user menu)
- [ ] Error boundaries (graceful error handling)
- [ ] 404 page (helpful navigation back to app)
- [ ] Favicon and meta tags (SEO)

#### Key Enhancements
- **Toast Notifications:** react-hot-toast library for non-intrusive feedback
- **Animations:** Subtle entrance/exit animations (100-200ms duration)
- **Mobile Navigation:** Bottom tab bar for key actions
- **Focus Management:** Return focus after modal close, skip links
- **Dark Mode:** System preference detection + manual toggle
- **Offline Support:** Display offline banner, queue mutations (optional)

#### Accessibility Checklist
- [ ] All images have alt text
- [ ] Form inputs have labels (visible or aria-label)
- [ ] Buttons have descriptive text or aria-label
- [ ] Focus visible on all interactive elements
- [ ] Keyboard navigation works without mouse
- [ ] Color contrast meets WCAG AA (4.5:1 for text)
- [ ] Screen reader tested (basic navigation)

---

## 🎯 User Flows

### Authentication Flow
```
┌─────────────────┐
│  Landing Page   │ (/)
│  (Login Form)   │
└────────┬────────┘
         │
    ┌────┴─────┐
    │          │
    ▼          ▼
┌─────────┐  ┌──────────────┐
│ Register│  │ Google OAuth │
└────┬────┘  └──────┬───────┘
     │              │
     │   ┌──────────▼───────────┐
     │   │ Google Consent Page  │
     │   └──────────┬───────────┘
     │              │
     └──────────────┴──────┐
                           ▼
                   ┌───────────────┐
                   │  Dashboard    │
                   │  (Books List) │
                   └───────────────┘

Login Success:
1. Store tokens (localStorage: accessToken, HttpOnly cookie: refreshToken)
2. Set user in auth store
3. Redirect to /dashboard

Token Refresh Flow:
1. Every API call checks token expiry
2. If expired, call /auth/refresh with cookie
3. Update accessToken in store
4. Retry original request
5. If refresh fails (401), logout and redirect to login
```

### Books Management Flow
```
┌──────────────────────────────────┐
│       Dashboard (Books List)      │
│                                   │
│  [Search Bar] [Status Tabs]      │
│  ┌──────┬──────┬──────┬──────┐  │
│  │ Book │ Book │ Book │ Book │  │
│  │ Card │ Card │ Card │ Card │  │
│  └──────┴──────┴──────┴──────┘  │
│         [Pagination]              │
│                          [+ Add]  │
└───┬──────────────────────────┬───┘
    │                          │
    │                          ▼
    │                   ┌─────────────┐
    │                   │ Add Book    │
    │                   │   Modal     │
    │                   └──────┬──────┘
    │                          │
    │                          ▼
    │                   ┌─────────────┐
    │                   │ Upload Cover│
    │                   │ Fill Form   │
    │                   │ Save        │
    │                   └──────┬──────┘
    │                          │
    ▼                          ▼
┌─────────────┐         [Dashboard Updated]
│ Book Detail │
│    Page     │
└──────┬──────┘
       │
  ┌────┴────┐
  │         │
  ▼         ▼
┌────┐   ┌──────┐
│Edit│   │Delete│
└─┬──┘   └───┬──┘
  │          │
  ▼          ▼
[Form]   [Confirm]
  │          │
  └────┬─────┘
       ▼
  [Dashboard
   Updated]
```

### Search & Filter Flow
```
Dashboard
    │
    ├─► Type in Search Bar (debounced 300ms)
    │   └─► API Call: /books/search?searchTerm=...
    │       └─► Update Books Grid
    │
    ├─► Click Status Tab (e.g., "Reading")
    │   └─► API Call: /books/search?status=1
    │       └─► Update Books Grid
    │
    └─► Combine: Search + Status
        └─► API Call: /books/search?searchTerm=...&status=1
            └─► Update Books Grid
```

---

## 📁 Project Structure

```
frontend/
├── public/
│   ├── favicon.ico
│   └── logo.svg
├── src/
│   ├── api/                      # API client and endpoint definitions
│   │   ├── client.ts             # Axios instance, interceptors
│   │   ├── endpoints/
│   │   │   ├── auth.ts           # Auth API calls
│   │   │   └── books.ts          # Books API calls
│   │   └── types.ts              # API request/response types
│   │
│   ├── components/               # React components
│   │   ├── ui/                   # Reusable UI primitives
│   │   │   ├── Button.tsx
│   │   │   ├── Input.tsx
│   │   │   ├── Modal.tsx
│   │   │   ├── Card.tsx
│   │   │   ├── Badge.tsx
│   │   │   ├── Select.tsx
│   │   │   ├── TextArea.tsx
│   │   │   ├── Spinner.tsx
│   │   │   └── Toast.tsx
│   │   │
│   │   ├── auth/                 # Authentication components
│   │   │   ├── LoginForm.tsx
│   │   │   ├── RegisterForm.tsx
│   │   │   └── GoogleLoginButton.tsx
│   │   │
│   │   ├── books/                # Book-related components
│   │   │   ├── BookCard.tsx
│   │   │   ├── BookGrid.tsx
│   │   │   ├── BookDetail.tsx
│   │   │   ├── BookForm.tsx
│   │   │   ├── BookCover.tsx
│   │   │   ├── AddBookModal.tsx
│   │   │   ├── EditBookModal.tsx
│   │   │   ├── DeleteBookModal.tsx
│   │   │   ├── StatusBadge.tsx
│   │   │   ├── ImageUpload.tsx
│   │   │   └── EmptyState.tsx
│   │   │
│   │   ├── layout/               # Layout components
│   │   │   ├── Header.tsx
│   │   │   ├── Navigation.tsx
│   │   │   ├── UserMenu.tsx
│   │   │   └── Footer.tsx
│   │   │
│   │   ├── search/               # Search components
│   │   │   ├── SearchBar.tsx
│   │   │   ├── StatusFilter.tsx
│   │   │   └── SearchResults.tsx
│   │   │
│   │   └── common/               # Common components
│   │       ├── Pagination.tsx
│   │       ├── LoadingSkeleton.tsx
│   │       ├── ErrorBoundary.tsx
│   │       └── ProtectedRoute.tsx
│   │
│   ├── hooks/                    # Custom React hooks
│   │   ├── auth/
│   │   │   ├── useAuth.ts        # Auth store wrapper
│   │   │   └── useGoogleLogin.ts
│   │   │
│   │   ├── books/
│   │   │   ├── useBooks.ts       # Fetch paginated books
│   │   │   ├── useBook.ts        # Fetch single book
│   │   │   ├── useAddBook.ts     # Add book mutation
│   │   │   ├── useUpdateBook.ts  # Update book mutation
│   │   │   ├── useDeleteBook.ts  # Delete book mutation
│   │   │   └── useSearchBooks.ts # Search books
│   │   │
│   │   └── utils/
│   │       ├── useDebounce.ts
│   │       ├── useLocalStorage.ts
│   │       └── useMediaQuery.ts
│   │
│   ├── pages/                    # Route pages
│   │   ├── LoginPage.tsx
│   │   ├── RegisterPage.tsx
│   │   ├── DashboardPage.tsx
│   │   ├── BookDetailPage.tsx
│   │   └── NotFoundPage.tsx
│   │
│   ├── store/                    # Zustand stores
│   │   ├── authStore.ts          # Auth state (user, tokens, login/logout)
│   │   └── uiStore.ts            # UI state (theme, sidebar open, etc.)
│   │
│   ├── utils/                    # Utility functions
│   │   ├── formatters.ts         # Date, number formatting
│   │   ├── validators.ts         # Custom validation functions
│   │   ├── constants.ts          # App constants
│   │   └── helpers.ts            # Misc helpers
│   │
│   ├── types/                    # TypeScript types
│   │   ├── auth.ts
│   │   ├── book.ts
│   │   └── common.ts
│   │
│   ├── styles/
│   │   └── index.css             # Tailwind imports + custom styles
│   │
│   ├── App.tsx                   # Root component, routing
│   ├── main.tsx                  # Entry point
│   └── vite-env.d.ts
│
├── .env.example                  # Environment variables template
├── .env.local                    # Local environment (gitignored)
├── .gitignore
├── index.html
├── package.json
├── postcss.config.js
├── tailwind.config.js
├── tsconfig.json
├── tsconfig.node.json
├── vite.config.ts
└── README.md
```

---

## ⚙️ Configuration Files

### Environment Variables (`.env.example`)
```bash
VITE_API_BASE_URL=http://localhost:5000
VITE_APP_NAME=IntelliShelf
VITE_GOOGLE_CLIENT_ID=your-google-client-id
```

### Tailwind Config (`tailwind.config.js`)
```javascript
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eef2ff',
          500: '#4f46e5',
          600: '#4338ca',
          700: '#3730a3',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
}
```

### Vite Config (`vite.config.ts`)
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
```

---

## 🔑 Key Technical Decisions

### 1. Why TanStack Query over manual state?
**Pros:**
- Automatic caching with configurable stale time
- Background refetching keeps data fresh
- Built-in loading/error states
- Optimistic updates with automatic rollback
- Request deduplication (prevents duplicate calls)
- Pagination/infinite scroll support

**Example:**
```typescript
const { data, isLoading, error } = useQuery({
  queryKey: ['books', page, pageSize],
  queryFn: () => fetchBooks({ page, pageSize }),
  staleTime: 5 * 60 * 1000, // 5 minutes
});
```

### 2. Why Zustand over Redux?
**Pros:**
- Minimal boilerplate (no actions, reducers, dispatch)
- Only ~1KB gzipped
- No context provider needed
- TypeScript-friendly
- Perfect for small global state (auth, theme)

**Example:**
```typescript
const useAuthStore = create<AuthState>((set) => ({
  user: null,
  login: async (email, password) => {
    const { user, accessToken } = await authApi.login(email, password);
    set({ user, accessToken, isAuthenticated: true });
  },
  logout: () => set({ user: null, accessToken: null, isAuthenticated: false }),
}));
```

### 3. Why Headless UI?
**Pros:**
- Fully accessible (ARIA, keyboard nav) out of the box
- Unstyled = complete design control with Tailwind
- Small bundle size
- Works seamlessly with React

**Components:**
- `<Dialog>` - Modals
- `<Menu>` - Dropdowns
- `<Listbox>` - Select dropdowns
- `<Combobox>` - Autocomplete
- `<Tab>` - Tabs

### 4. File Upload Strategy
**Multipart Form Data:**
```typescript
const formData = new FormData();
formData.append('title', 'The Great Gatsby');
formData.append('authors', 'F. Scott Fitzgerald');
formData.append('imageFile', fileInput.files[0]);

await axios.post('/books', formData, {
  headers: { 'Content-Type': 'multipart/form-data' },
});
```

**Client-side preview:**
```typescript
const [preview, setPreview] = useState<string | null>(null);

const handleFileChange = (file: File) => {
  const reader = new FileReader();
  reader.onloadend = () => setPreview(reader.result as string);
  reader.readAsDataURL(file);
};
```

### 5. Google OAuth Flow
**Frontend:**
```typescript
const handleGoogleLogin = () => {
  const returnUrl = encodeURIComponent('/dashboard');
  window.location.href = `${API_URL}/auth/google?returnUrl=${returnUrl}`;
};
```

**Backend handles:**
1. Redirects to Google consent page
2. User approves
3. Google redirects to `/auth/google/callback`
4. Backend sets refresh token cookie
5. Backend redirects to `returnUrl` (frontend)
6. Frontend calls `/auth/me` to get user data

### 6. Token Refresh Strategy
**Interceptor approach:**
```typescript
axiosInstance.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const { accessToken } = await authApi.refresh();
        useAuthStore.getState().setAccessToken(accessToken);
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return axiosInstance(originalRequest);
      } catch (refreshError) {
        useAuthStore.getState().logout();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);
```

---

## 🎨 Component Design Examples

### Book Card
```typescript
interface BookCardProps {
  book: Book;
  onEdit: (book: Book) => void;
  onDelete: (bookId: string) => void;
}

export const BookCard: React.FC<BookCardProps> = ({ book, onEdit, onDelete }) => {
  return (
    <div className="group relative bg-white rounded-lg shadow-sm hover:shadow-md transition-shadow overflow-hidden">
      {/* Cover Image */}
      <div className="aspect-[2/3] bg-gray-100">
        {book.coverImageUrl ? (
          <img
            src={book.coverImageUrl}
            alt={book.title}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-400">
            <BookIcon size={48} />
          </div>
        )}
      </div>

      {/* Overlay on hover */}
      <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-50 transition-opacity flex items-center justify-center gap-2 opacity-0 group-hover:opacity-100">
        <Button variant="secondary" size="sm" onClick={() => onEdit(book)}>
          <EditIcon size={16} />
        </Button>
        <Button variant="danger" size="sm" onClick={() => onDelete(book.id)}>
          <TrashIcon size={16} />
        </Button>
      </div>

      {/* Info */}
      <div className="p-4">
        <h3 className="font-semibold text-gray-900 truncate">{book.title}</h3>
        <p className="text-sm text-gray-600 truncate">{book.authors}</p>
        <StatusBadge status={book.status} className="mt-2" />
      </div>
    </div>
  );
};
```

### Status Badge
```typescript
const statusConfig = {
  0: { label: 'Unread', color: 'bg-gray-100 text-gray-700' },
  1: { label: 'Reading', color: 'bg-blue-100 text-blue-700' },
  2: { label: 'Read', color: 'bg-green-100 text-green-700' },
};

export const StatusBadge: React.FC<{ status: number }> = ({ status }) => {
  const config = statusConfig[status as keyof typeof statusConfig];

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.color}`}>
      {config.label}
    </span>
  );
};
```

---

## 📊 Performance Considerations

### Code Splitting
```typescript
// Lazy load pages
const DashboardPage = lazy(() => import('./pages/DashboardPage'));
const BookDetailPage = lazy(() => import('./pages/BookDetailPage'));

// Wrap in Suspense
<Suspense fallback={<LoadingSpinner />}>
  <Routes>
    <Route path="/dashboard" element={<DashboardPage />} />
    <Route path="/books/:id" element={<BookDetailPage />} />
  </Routes>
</Suspense>
```

### Image Optimization
- Use `loading="lazy"` for images below the fold
- Serve responsive images (srcset) if backend supports
- Show low-quality placeholders during load

### Bundle Size
- Tree-shake unused libraries
- Use Vite's rollup optimization
- Analyze bundle with `vite-bundle-visualizer`

---

## 🧪 Testing Strategy (Future)

### Unit Tests (Vitest)
- Component rendering
- Hook logic
- Utility functions

### Integration Tests (React Testing Library)
- User flows (login, add book, search)
- Form validation
- Error states

### E2E Tests (Playwright)
- Critical user journeys
- Cross-browser testing

---

## 🚀 Deployment

### Build Command
```bash
npm run build
# Output: dist/
```

### Hosting Options
- **Vercel** - Zero-config, automatic HTTPS, preview deployments
- **Netlify** - Similar to Vercel, great for SPAs
- **AWS S3 + CloudFront** - Scalable, cost-effective
- **Azure Static Web Apps** - Integrates with backend if both on Azure

### Environment Variables
Set in hosting platform:
- `VITE_API_BASE_URL` - Production API URL
- `VITE_GOOGLE_CLIENT_ID` - Google OAuth client ID

---

## 📝 Future Enhancements (Post-MVP)

### Phase 5+
- [ ] **AI-Powered Features**
  - Parse books from photos (OCR + AI endpoint)
  - Book recommendations based on library
  - Auto-tagging with AI

- [ ] **Social Features**
  - Share books with friends
  - Book clubs / reading lists
  - Public profile page

- [ ] **Advanced Search**
  - Filters: Publication year range, page count, tags
  - Saved searches
  - Export search results (CSV, PDF)

- [ ] **Data Visualization**
  - Reading stats dashboard
  - Books read per month/year charts
  - Genre breakdown pie chart

- [ ] **PWA Features**
  - Offline mode with service workers
  - Install as app on mobile
  - Push notifications (reading reminders)

- [ ] **Integrations**
  - Import from Goodreads
  - Sync with Kindle library
  - Barcode scanner (ISBN lookup)

---

## 📚 Resources & References

### Documentation
- [React Docs](https://react.dev)
- [TanStack Query](https://tanstack.com/query/latest/docs/react/overview)
- [Zustand](https://docs.pmnd.rs/zustand/getting-started/introduction)
- [Tailwind CSS](https://tailwindcss.com/docs)
- [Headless UI](https://headlessui.com)
- [React Hook Form](https://react-hook-form.com)

### Design Inspiration
- [Dribbble - Book Apps](https://dribbble.com/search/book-app)
- [Goodreads](https://www.goodreads.com)
- [Literal Club](https://literal.club)

---

## ✅ Success Criteria

### Phase 1 Complete When:
- ✅ User can register with email/password
- ✅ User can login with email/password
- ✅ User can login with Google (redirect flow)
- ✅ Auth state persists on page refresh
- ✅ Protected routes redirect to login if not authenticated
- ✅ UI is clean, responsive, and follows design system

### Phase 2 Complete When:
- ✅ User can view all books in paginated grid
- ✅ User can add a book with cover image
- ✅ User can edit a book
- ✅ User can delete a book with confirmation
- ✅ User can change reading status
- ✅ All CRUD operations work with proper error handling

### Phase 3 Complete When:
- ✅ User can search books with live results
- ✅ User can filter by reading status
- ✅ Search and filters work together
- ✅ Search persists in URL (shareable)

### Phase 4 Complete When:
- ✅ All actions show toast notifications
- ✅ App is fully responsive (mobile, tablet, desktop)
- ✅ Accessibility audit passes
- ✅ App handles errors gracefully
- ✅ Production build is optimized (<500KB initial bundle)

---

## 📞 Support

For questions or issues during implementation:
- Check backend API docs: `/home/user/intellishelf-api/AGENTS.md`
- Review this plan: `/home/user/intellishelf-api/FRONTEND_PLAN.md`
- Test API endpoints with Thunder Client or Postman

---

**Last Updated:** 2025-11-16
**Version:** 1.0
**Status:** Phase 1 - In Progress