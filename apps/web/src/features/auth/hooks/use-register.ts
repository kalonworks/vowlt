import { useMutation } from "@tanstack/react-query";
import { authApi } from "../api/auth";
import type { RegisterRequest } from "../types";

export const useRegister = () => {
  return useMutation({
    mutationFn: (data: RegisterRequest) => authApi.register(data),
  });
};
