import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import api from '@/lib/api';
import type { User, LoginDto, RegisterDto, LoginResult, UserResponse } from '@/types/auth';

export const useAuth = () => {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

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
    mutationFn: () => api.post<void>('/auth/logout', {}),
    onSuccess: () => {
      queryClient.clear(); // Clear all cached data
      toast.success('Logged out successfully');
      navigate('/auth');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Logout failed');
    },
  });

  // Delete account mutation
  const deleteAccount = useMutation({
    mutationFn: () => api.delete<void>('/auth/account'),
    onSuccess: () => {
      // Clear all cached data
      queryClient.clear();
      toast.success('Account deleted successfully');
      // Redirect to auth page
      navigate('/auth');
    },
    onError: (error: Error) => {
      toast.error(`Failed to delete account: ${error.message}`);
    },
  });

  return {
    user,
    isLoading,
    isAuthenticated: !!user,
    login,
    register,
    logout,
    deleteAccount,
  };
};
