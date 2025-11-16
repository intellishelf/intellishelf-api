import { AxiosError } from 'axios';

/**
 * API error response structure from the backend
 */
export interface ApiErrorResponse {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  status?: number;
}

/**
 * Extracts a user-friendly error message from an API error response
 * Handles both .NET ProblemDetails format and custom error responses
 *
 * @param error - The error object (typically from Axios)
 * @param fallback - Default message if no specific error can be extracted
 * @returns A user-friendly error message
 */
export const extractErrorMessage = (
  error: unknown,
  fallback: string = 'An unexpected error occurred'
): string => {
  // Handle AxiosError specifically
  if (error && typeof error === 'object' && 'response' in error) {
    const axiosError = error as AxiosError<ApiErrorResponse>;
    const data = axiosError.response?.data;

    if (data) {
      // Try to get error message in order of precedence
      if (data.title) return data.title;
      if (data.detail) return data.detail;

      // Handle validation errors (multiple fields)
      if (data.errors && typeof data.errors === 'object') {
        const firstError = Object.values(data.errors)[0];
        if (Array.isArray(firstError) && firstError.length > 0) {
          return firstError[0];
        }
      }
    }
  }

  // Handle standard Error objects
  if (error instanceof Error) {
    return error.message || fallback;
  }

  return fallback;
};

/**
 * Type guard to check if an error is an AxiosError
 */
export const isAxiosError = (error: unknown): error is AxiosError => {
  return error !== null && typeof error === 'object' && 'isAxiosError' in error;
};
