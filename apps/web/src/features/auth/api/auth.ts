import { apiClient } from "@/lib/api-client";
import type {
  RegisterRequest,
  RefreshTokenRequest,
  AuthResponse,
  User,
} from "../types";

// Modern pattern: separate API functions from hooks
export const authApi = {
  register: async (
    data: RegisterRequest
  ): Promise<{ success: boolean; message: string; user: User }> => {
    const response = await apiClient.post<{
      success: boolean;
      message: string;
      user: User;
    }>("/api/auth/register", data);
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
