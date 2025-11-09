import { apiClient } from "@/lib/api-client";
import type {
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
  AuthResponse,
} from "../types";

// Modern pattern: separate API functions from hooks
export const authApi = {
  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>(
      "/api/auth/register",
      data
    );
    return response.data;
  },

  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>(
      "/api/auth/login",
      data
    );
    return response.data;
  },

  refresh: async (data: RefreshTokenRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>(
      "/api/auth/refresh",
      data
    );
    return response.data;
  },

  logout: async (): Promise<void> => {
    await apiClient.post("/api/auth/logout");
  },
};
