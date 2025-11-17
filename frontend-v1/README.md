# IntelliShelf Frontend

A modern, minimalistic React application for managing your personal book collection.

## 🚀 Tech Stack

- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **TailwindCSS** - Utility-first CSS framework
- **React Router v6** - Client-side routing
- **Zustand** - State management
- **TanStack Query** - Server state management (coming in Phase 2)
- **React Hook Form** - Form handling (coming in Phase 2)
- **Zod** - Schema validation (coming in Phase 2)
- **Axios** - HTTP client
- **Headless UI** - Accessible UI components
- **Lucide React** - Icon library
- **React Hot Toast** - Notifications

## 📋 Prerequisites

- Node.js 18+ and npm
- Backend API running (see `../README.md`)

## 🛠️ Installation

1. Install dependencies:
```bash
npm install
```

2. Create `.env.local` file (copy from `.env.example`):
```bash
cp .env.example .env.local
```

3. Update `.env.local` with your backend API URL:
```env
VITE_API_BASE_URL=http://localhost:5000
VITE_APP_NAME=IntelliShelf
```

## 🏃 Running the App

### Development Mode
```bash
npm run dev
```

The app will open automatically at `http://localhost:3000`.

### Build for Production
```bash
npm run build
```

Build output will be in the `dist/` directory.

### Preview Production Build
```bash
npm run preview
```

## 📁 Project Structure

```
src/
├── api/                    # API client and endpoints
│   ├── client.ts          # Axios instance with interceptors
│   └── endpoints/         # API endpoint functions
├── components/            # React components
│   ├── ui/               # Reusable UI components
│   ├── auth/             # Authentication forms
│   ├── books/            # Book components (Phase 2)
│   ├── layout/           # Layout components
│   └── common/           # Common components
├── hooks/                # Custom React hooks
├── pages/                # Route pages
├── store/                # Zustand stores
├── types/                # TypeScript types
├── utils/                # Utility functions
├── App.tsx               # Root component with routing
└── main.tsx              # Entry point
```

## 🎨 Features

### Phase 1 (✅ Complete)
- ✅ User registration with email/password
- ✅ User login with email/password
- ✅ Google OAuth login (redirect flow)
- ✅ Protected routes
- ✅ Token-based authentication with auto-refresh
- ✅ Responsive design
- ✅ Toast notifications
- ✅ Clean, modern UI

### Phase 2 (Coming Soon)
- 📚 Books list with pagination
- ➕ Add/Edit/Delete books
- 🖼️ Image upload for book covers
- 📊 Reading status tracking
- 🔍 Search and filtering

## 🔐 Authentication Flow

1. User registers or logs in
2. Backend returns access token and refresh token (cookie)
3. Access token stored in localStorage
4. All API requests include access token in Authorization header
5. If access token expires (401), automatically refresh using cookie
6. If refresh fails, redirect to login

## 🎯 Key Components

### API Client (`src/api/client.ts`)
- Axios instance with base URL configuration
- Request interceptor: Adds JWT token to all requests
- Response interceptor: Auto-refresh on 401 errors
- Token management utilities

### Auth Store (`src/store/authStore.ts`)
- Global authentication state with Zustand
- Login, register, logout actions
- User data management
- Loading and error states

### Protected Route (`src/components/common/ProtectedRoute.tsx`)
- Wrapper for authenticated routes
- Redirects to login if not authenticated
- Shows loading spinner while checking auth

### UI Components (`src/components/ui/`)
- **Button**: Multiple variants (primary, secondary, outline, ghost, danger)
- **Input**: With label, error, and helper text support
- **Card**: Flexible card container
- **Spinner**: Loading indicators

## 🌐 Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_API_BASE_URL` | Backend API URL | `http://localhost:5000` |
| `VITE_APP_NAME` | Application name | `IntelliShelf` |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth client ID (optional) | - |

## 📝 Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## 🎨 Design System

### Colors
- **Primary**: Indigo (#4F46E5) - CTAs, links, active states
- **Neutral**: Gray scale - Text, backgrounds, borders
- **Success**: Green (#10B981) - Success messages
- **Error**: Red (#EF4444) - Error messages
- **Warning**: Amber (#F59E0B) - Warnings

### Typography
- Font: Inter (Google Fonts)
- Scale: Tailwind's default type scale

### Spacing
- Follows Tailwind's spacing scale (4px increments)

## 🔧 Development Tips

1. **Hot Module Replacement (HMR)**: Changes reflect instantly in the browser
2. **TypeScript**: All API responses are typed for better developer experience
3. **Tailwind IntelliSense**: Install the VSCode extension for autocomplete
4. **React DevTools**: Install browser extension for debugging

## 🐛 Troubleshooting

### CORS Errors
Make sure the backend API allows requests from `http://localhost:3000`. Check the backend's CORS configuration.

### 401 Unauthorized
- Check if backend API is running
- Verify `VITE_API_BASE_URL` in `.env.local`
- Clear localStorage and cookies, then login again

### Build Errors
```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

## 📚 Learn More

- [React Documentation](https://react.dev)
- [Vite Documentation](https://vitejs.dev)
- [TailwindCSS Documentation](https://tailwindcss.com)
- [React Router Documentation](https://reactrouter.com)
- [Zustand Documentation](https://docs.pmnd.rs/zustand)

## 📄 License

This project is part of the IntelliShelf application.
