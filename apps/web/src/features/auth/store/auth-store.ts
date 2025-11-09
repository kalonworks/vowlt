import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { User } from "../types";

interface AuthState {
  // State
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;

  // Computed
  isAuthenticated: boolean;

  // Actions
  setAuth: (user: User, accessToken: string, refreshToken: string) => void;
  clearAuth: () => void;
}

// persist middleware
export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      // Initial state
      user: null,
      accessToken: null,
      refreshToken: null,

      // Computed values
      get isAuthenticated() {
        return !!get().accessToken && !!get().user;
      },

      // Actions
      setAuth: (user, accessToken, refreshToken) => {
        // Also store in localStorage for api-client interceptor
        localStorage.setItem("accessToken", accessToken);
        localStorage.setItem("refreshToken", refreshToken);

        set({ user, accessToken, refreshToken });
      },

      clearAuth: () => {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");

        set({ user: null, accessToken: null, refreshToken: null });
      },
    }),
    {
      name: "auth-storage", // localStorage key
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
      }),
    }
  )
);
