import { QueryClient } from "@tanstack/react-query";
import type { AxiosError } from "axios";
import type { ApiError } from "./api-client";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Stale time: how long data is considered fresh (5 minutes)
      staleTime: 1000 * 60 * 5,
      // Retry failed requests (not for 401/403/404)
      retry: (failureCount, error) => {
        const axiosError = error as AxiosError<ApiError>;
        const status = axiosError.response?.status;
        if (status === 401 || status === 403 || status === 404) {
          return false;
        }
        return failureCount < 2;
      },
      // Refetch on window focus (good UX)
      refetchOnWindowFocus: true,
    },
    mutations: {
      onError: (error) => {
        const axiosError = error as AxiosError<ApiError>;
        console.error("Mutation error:", axiosError.response?.data);
      },
    },
  },
});
