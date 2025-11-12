import { startOAuthFlow } from "../services/oauth";

export const LoginPage = () => {
  const handleLogin = () => {
    startOAuthFlow();
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full space-y-8 p-8 bg-white rounded-lg shadow">
        <div>
          <h2 className="text-center text-3xl font-bold">Welcome to Vowlt</h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Your semantic bookmark search
          </p>
        </div>

        <div className="mt-8 space-y-6">
          <button
            onClick={handleLogin}
            className="w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm 
    font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 
    focus:ring-blue-500 transition-colors"
          >
            Sign in with Vowlt
          </button>

          <div className="text-center">
            <p className="text-xs text-gray-500">
              Secure OAuth 2.1 authentication with PKCE
            </p>
          </div>
        </div>

        <p className="text-center text-sm text-gray-600">
          Don't have an account?{" "}
          <a href="/register" className="text-blue-600 hover:text-blue-500">
            Sign up
          </a>
        </p>
      </div>
    </div>
  );
};
