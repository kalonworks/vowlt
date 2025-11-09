import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";

// API Error type matching your backend's ErrorResponse
export interface ApiError {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
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
    const token = localStorage.getItem("accessToken");
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
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
        const refreshToken = localStorage.getItem("refreshToken");
        if (!refreshToken) {
          // No refresh token, clear state and redirect
          localStorage.removeItem("accessToken");
          localStorage.removeItem("refreshToken");
          window.location.href = "/login";
          return Promise.reject(error);
        }

        // Call refresh endpoint
        const { data } = await axios.post<{
          accessToken: string;
          refreshToken: string;
        }>(
          `${import.meta.env.VITE_API_BASE_URL}/api/auth/refresh`,
          { refreshToken },
          {
            headers: { "Content-Type": "application/json" },
          }
        );

        // Save new tokens
        localStorage.setItem("accessToken", data.accessToken);
        localStorage.setItem("refreshToken", data.refreshToken);

        // Retry original request with new token
        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        }
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh failed, logout
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        window.location.href = "/login";
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);
