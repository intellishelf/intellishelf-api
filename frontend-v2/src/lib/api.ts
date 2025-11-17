export const API_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

// Helper to build query params (handles arrays, null/undefined)
const buildQueryParams = (params: Record<string, any>): URLSearchParams => {
  const searchParams = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    // Skip null/undefined
    if (value == null) return;

    // Handle arrays (e.g., tags, multiple filters)
    if (Array.isArray(value)) {
      value.forEach((v) => searchParams.append(key, String(v)));
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

    const res = await fetch(url, {
      credentials: 'include', // Send cookies for refresh token
      headers: { 'Content-Type': 'application/json' },
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error || `HTTP ${res.status}: ${res.statusText}`);
    }

    return res.json();
  },

  post: async <T>(endpoint: string, body?: any): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: body ? JSON.stringify(body) : undefined,
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error || `HTTP ${res.status}: ${res.statusText}`);
    }

    return res.json();
  },

  put: async <T>(endpoint: string, body: any): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error || `HTTP ${res.status}: ${res.statusText}`);
    }

    return res.json();
  },

  delete: async <T>(endpoint: string): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'DELETE',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error || `HTTP ${res.status}: ${res.statusText}`);
    }

    return res.json();
  },

  upload: async <T>(endpoint: string, formData: FormData): Promise<T> => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: 'POST',
      credentials: 'include',
      body: formData, // No Content-Type header - browser sets it with boundary
    });

    if (!res.ok) {
      const error = await res.text();
      throw new Error(error || `HTTP ${res.status}: ${res.statusText}`);
    }

    return res.json();
  },
};

export default api;
