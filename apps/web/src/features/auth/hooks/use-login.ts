import { useMutation } from "@tanstack/react-query";
import { useNavigate, useRouter } from "@tanstack/react-router";
import { authApi } from "../api/auth";
import { useAuthStore } from "../store/auth-store";

export const useLogin = () => {
  const navigate = useNavigate();
  const router = useRouter();
  const setAuth = useAuthStore((state) => state.setAuth);

  return useMutation({
    mutationFn: authApi.login,
    onSuccess: async (data) => {
      setAuth(data.user, data.accessToken, data.refreshToken);
      await router.invalidate();
      await navigate({ to: "/bookmarks" });
    },
  });
};
