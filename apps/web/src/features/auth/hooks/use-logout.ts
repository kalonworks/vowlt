import { useMutation } from "@tanstack/react-query";
import { useNavigate, useRouter } from "@tanstack/react-router";
import { authApi } from "../api/auth";
import { useAuthStore } from "../store/auth-store";
import { queryClient } from "@/lib/query-client";

export const useLogout = () => {
  const navigate = useNavigate();
  const router = useRouter();
  const clearAuth = useAuthStore((state) => state.clearAuth);

  return useMutation({
    mutationFn: authApi.logout,
    onSuccess: async () => {
      clearAuth();
      queryClient.clear();
      await router.invalidate();
      await navigate({ to: "/login" });
    },
    onError: async () => {
      clearAuth();
      queryClient.clear();
      await router.invalidate();
      await navigate({ to: "/login" });
    },
  });
};
