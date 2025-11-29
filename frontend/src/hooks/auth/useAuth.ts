import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import api from '@/lib/api';
import type { User, LoginDto, RegisterDto, LoginResult, UserResponse } from '@/types/auth';

export const useAuth = () => {
  const queryClient = useQueryClient();

  // Get current user (cached)
  const { data: user, isLoading } = useQuery<UserResponse>({
    queryKey: ['auth', 'me'],
    queryFn: () => api.get<UserResponse>('/auth/me'),
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 min
  });

  // Login mutation
  const login = useMutation({
    mutationFn: (data: LoginDto) => api.post<LoginResult>('/auth/login', data),
    onSuccess: async (result) => {
      // After login, fetch user data
      const userData = await api.get<UserResponse>('/auth/me');
      queryClient.setQueryData(['auth', 'me'], userData);
      toast.success('Welcome back!');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Login failed');
    },
  });

  // Register mutation
  const register = useMutation({
    mutationFn: (data: RegisterDto) => api.post<LoginResult>('/auth/register', data),
    onSuccess: async (result) => {
      // After registration, fetch user data
      const userData = await api.get<UserResponse>('/auth/me');
      queryClient.setQueryData(['auth', 'me'], userData);
      toast.success('Account created successfully!');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Registration failed');
    },
  });

  // Logout mutation
  const logout = useMutation({
    mutationFn: () => api.void('/auth/logout', 'POST'),
    onSuccess: () => {
      queryClient.setQueryData(['auth', 'me'], null);
      queryClient.clear(); // Clear all cached data
      toast.success('Logged out successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Logout failed');
    },
  });

  return {
    user,
    isLoading,
    isAuthenticated: !!user,
    login,
    register,
    logout,
  };
};
