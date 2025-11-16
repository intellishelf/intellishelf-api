import { create } from 'zustand';
import type { User, LoginRequest, RegisterRequest } from '../types/auth';
import { authApi } from '../api/endpoints/auth';
import { setAccessToken, getAccessToken } from '../api/client';
import { extractErrorMessage } from '../utils/errors';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  // Actions
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  loadUser: () => Promise<void>;
  clearError: () => void;
  setUser: (user: User | null) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: true,
  error: null,

  login: async (data: LoginRequest) => {
    try {
      set({ isLoading: true, error: null });

      const response = await authApi.login(data);
      setAccessToken(response.accessToken);

      // Fetch user data after login
      const user = await authApi.getCurrentUser();

      set({
        user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error) {
      const errorMessage = extractErrorMessage(error, 'Login failed. Please try again.');
      set({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: errorMessage,
      });
      throw error;
    }
  },

  register: async (data: RegisterRequest) => {
    try {
      set({ isLoading: true, error: null });

      const response = await authApi.register(data);
      setAccessToken(response.accessToken);

      // Fetch user data after registration
      const user = await authApi.getCurrentUser();

      set({
        user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error) {
      const errorMessage = extractErrorMessage(error, 'Registration failed. Please try again.');
      set({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: errorMessage,
      });
      throw error;
    }
  },

  logout: async () => {
    try {
      await authApi.logout();
    } catch (error) {
      // Ignore logout errors, still clear local state
      console.error('Logout error:', error);
    } finally {
      setAccessToken(null);
      set({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  },

  loadUser: async () => {
    const token = getAccessToken();

    if (!token) {
      set({ isLoading: false, isAuthenticated: false, user: null });
      return;
    }

    try {
      set({ isLoading: true });

      const user = await authApi.getCurrentUser();

      set({
        user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error) {
      // Token is invalid or expired
      setAccessToken(null);
      set({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  },

  clearError: () => set({ error: null }),

  setUser: (user: User | null) => {
    set({
      user,
      isAuthenticated: !!user,
    });
  },
}));
