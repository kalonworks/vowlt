import { RouterProvider, createRouter } from "@tanstack/react-router";
import { routeTree } from "./routeTree.gen";
import {
  useAuthStore,
  selectIsAuthenticated,
} from "@/features/auth/store/auth-store";

// Create router instance with initial context
const router = createRouter({
  routeTree,
  context: {
    auth: {
      isAuthenticated: false,
      user: null,
    },
  },
});

// Register router for type-safety
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

function InnerApp() {
  // Use selector function instead of getter
  const isAuthenticated = useAuthStore(selectIsAuthenticated);
  const user = useAuthStore((state) => state.user);

  console.log("InnerApp render - isAuthenticated:", isAuthenticated);
  console.log("InnerApp render - user:", user);

  return (
    <RouterProvider
      router={router}
      context={{
        auth: {
          isAuthenticated,
          user,
        },
      }}
    />
  );
}

function App() {
  return <InnerApp />;
}

export default App;
