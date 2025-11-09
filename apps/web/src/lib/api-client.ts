import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";

// API Error type matching your backend's ErrorResponse
export interface ApiError {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

// Helper to get auth state from Zustand persist storage
function getAuthFromStorage() {
  const authStorage = localStorage.getItem("auth-storage");
  if (!authStorage) return null;

  try {
    const { state } = JSON.parse(authStorage);
    return {
      accessToken: state.accessToken,
      refreshToken: state.refreshToken,
    };
  } catch {
    return null;
  }
}

// Helper to update tokens in Zustand persist storage
function updateTokensInStorage(accessToken: string, refreshToken: string) {
  const authStorage = localStorage.getItem("auth-storage");
  if (!authStorage) return;

  try {
    const parsed = JSON.parse(authStorage);
    parsed.state.accessToken = accessToken;
    parsed.state.refreshToken = refreshToken;
    localStorage.setItem("auth-storage", JSON.stringify(parsed));
  } catch {
    console.error("Failed to update tokens in storage");
  }
}

// Create axios instance
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor - inject JWT token
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const auth = getAuthFromStorage();
    if (auth?.accessToken && config.headers) {
      config.headers.Authorization = `Bearer ${auth.accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handle token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiError>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    // If 401 and we haven't retried yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const auth = getAuthFromStorage();
        if (!auth?.refreshToken) {
          // No refresh token, clear and redirect
          localStorage.removeItem("auth-storage");
          window.location.href = "/login";
          return Promise.reject(error);
        }

        // Call refresh endpoint
        const { data } = await axios.post<{
          accessToken: string;
          refreshToken: string;
        }>(
          `${import.meta.env.VITE_API_BASE_URL}/api/auth/refresh`,
          { refreshToken: auth.refreshToken },
          {
            headers: { "Content-Type": "application/json" },
          }
        );

        // Update tokens in Zustand persist storage
        updateTokensInStorage(data.accessToken, data.refreshToken);

        // Retry original request with new token
        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        }
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh failed, logout
        localStorage.removeItem("auth-storage");
        window.location.href = "/login";
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);
