import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig(() => {
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
