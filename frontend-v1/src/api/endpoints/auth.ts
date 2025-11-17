import { apiClient } from '../client';
import type {
  LoginRequest,
  RegisterRequest,
  LoginResponse,
  User,
  RefreshTokenRequest,
  RevokeTokenRequest,
} from '../../types/auth';

export const authApi = {
  // Register a new user
  register: async (data: RegisterRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/register', data);
    return response.data;
  },

  // Login with email and password
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/login', data);
    return response.data;
  },

  // Refresh access token using refresh token cookie
  refresh: async (data?: RefreshTokenRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/refresh', data || {});
    return response.data;
  },

  // Revoke refresh token
  revoke: async (data: RevokeTokenRequest): Promise<void> => {
    await apiClient.post('/auth/revoke', data);
  },

  // Logout (revokes refresh token and clears cookie)
  logout: async (): Promise<void> => {
    await apiClient.post('/auth/logout');
  },

  // Get current authenticated user
  getCurrentUser: async (): Promise<User> => {
    const response = await apiClient.get<User>('/auth/me');
    return response.data;
  },

  // Initiate Google OAuth login
  googleLogin: (returnUrl: string = '/dashboard'): void => {
    const encodedReturnUrl = encodeURIComponent(returnUrl);
    window.location.href = `${apiClient.defaults.baseURL}/auth/google?returnUrl=${encodedReturnUrl}`;
  },
};
