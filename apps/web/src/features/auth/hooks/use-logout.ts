import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { authApi } from "../api/auth";
import { useAuthStore } from "../store/auth-store";
import { queryClient } from "@/lib/query-client";

export const useLogout = () => {
  const navigate = useNavigate();
  const clearAuth = useAuthStore((state) => state.clearAuth);

  return useMutation({
    mutationFn: authApi.logout,
    onSuccess: async () => {
      clearAuth();
      // Clear all cached queries
      queryClient.clear();
      // Navigate to login
      await navigate("/login");
    },
    onError: async () => {
      clearAuth();
      queryClient.clear();
      await navigate("/login");
    },
  });
};
