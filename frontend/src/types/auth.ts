// User information from backend
export interface User {
  id: string;
  email: string;
}

// Login request payload
export interface LoginDto {
  email: string;
  password: string;
}

// Register request payload
export interface RegisterDto {
  email: string;
  password: string;
}

// Login/Register response from backend
export interface LoginResult {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  refreshTokenExpiry: string;
}

// User response from /auth/me endpoint
export interface UserResponse {
  id: string;
  email: string;
}
