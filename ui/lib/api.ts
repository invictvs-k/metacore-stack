/**
 * API Service Layer
 * Centralized API calls to the backend server
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  status: number;
}

/**
 * Type Definitions
 */
export interface User {
  id: string;
  email: string;
  name: string;
  createdAt?: string;
}

export interface Room {
  id: string;
  name: string;
  created: string;
  artifactCount?: number;
}

export interface Artifact {
  name: string;
  type: string;
  version: number;
  sha256: string;
  created: string;
  metadata?: Record<string, string>;
}

/**
 * Generic fetch wrapper with error handling
 */
async function fetchApi<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  try {
    const url = `${API_BASE_URL}${endpoint}`;
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    const data = await response.json().catch(() => null);

    return {
      data: response.ok ? data : undefined,
      error: !response.ok ? data?.message || 'Request failed' : undefined,
      status: response.status,
    };
  } catch (error) {
    console.error('API Error:', error);
    return {
      error: 'Network error or server unavailable',
      status: 0,
    };
  }
}

/**
 * Authentication API
 */
export const authApi = {
  login: async (email: string, password: string) => {
    return fetchApi<{ token: string; user: User }>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  },

  register: async (name: string, email: string, password: string) => {
    return fetchApi<{ token: string; user: User }>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ name, email, password }),
    });
  },

  logout: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token');
    }
  },

  getToken: (): string | null => {
    if (typeof window !== 'undefined') {
      return localStorage.getItem('auth_token');
    }
    return null;
  },
};

/**
 * Rooms API
 */
export const roomsApi = {
  list: async () => {
    return fetchApi<{ rooms: Room[] }>('/api/rooms');
  },

  get: async (roomId: string) => {
    return fetchApi<Room>(`/rooms/${roomId}`);
  },

  create: async (name: string) => {
    return fetchApi<{ room: Room }>('/api/rooms', {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
  },

  listArtifacts: async (roomId: string, entityId: string = 'demo-entity') => {
    return fetchApi<{ items: Artifact[] }>(`/rooms/${roomId}/artifacts`, {
      headers: {
        'X-Entity-Id': entityId,
      },
    });
  },
};

/**
 * Health check
 */
export const healthApi = {
  check: async () => {
    return fetchApi<{ status: string }>('/health');
  },
};

export default {
  auth: authApi,
  rooms: roomsApi,
  health: healthApi,
};
