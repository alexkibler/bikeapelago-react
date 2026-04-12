import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  // Load env file based on `mode` in the current working directory.
  // Set the third parameter to '' to load all env instead of just those starting with `VITE_`.
  const apiUrl = process.env.VITE_PUBLIC_API_URL || process.env.VITE_API_URL || 'http://127.0.0.1:5054';
  
  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target: apiUrl,
          changeOrigin: true,
        },
        '/hubs': {
          target: apiUrl,
          ws: true,
          changeOrigin: true,
        }
      }
    }
  };
});
