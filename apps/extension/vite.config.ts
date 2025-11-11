import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "path";

export default defineConfig({
  plugins: [react()],

  build: {
    outDir: "dist",
    emptyOutDir: true,

    rollupOptions: {
      input: {
        // Popup entry point
        popup: resolve(__dirname, "popup.html"),
        // Background service worker entry point
        background: resolve(__dirname, "src/background/service-worker.ts"),
      },
      output: {
        entryFileNames: (chunkInfo) => {
          // Background script goes to root of dist
          if (chunkInfo.name === "background") {
            return "background.js";
          }
          // Popup assets go to assets folder
          return "assets/[name]-[hash].js";
        },
        chunkFileNames: "assets/[name]-[hash].js",
        assetFileNames: "assets/[name]-[hash].[ext]",
      },
    },
  },

  // Resolve aliases (optional, for cleaner imports)
  resolve: {
    alias: {
      "@": resolve(__dirname, "./src"),
    },
  },
});
