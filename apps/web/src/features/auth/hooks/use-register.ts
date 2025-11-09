import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { authApi } from "../api/auth";
import { useAuthStore } from "../store/auth-store";

export const useRegister = () => {
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);

  return useMutation({
    mutationFn: authApi.register,
    onSuccess: async (data) => {
      setAuth(data.user, data.accessToken, data.refreshToken);
      await navigate("/bookmarks");
    },
  });
};
