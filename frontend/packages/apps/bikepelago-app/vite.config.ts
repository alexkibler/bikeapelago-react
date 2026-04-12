<<<<<<< HEAD
/// <reference types="vitest" />
import { defineConfig, loadEnv } from 'vite';
=======
import { defineConfig } from 'vite';
>>>>>>> 0f70b2a (Docker compose touchups)
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
<<<<<<< HEAD
  const env = loadEnv(mode, process.cwd(), '');
  const apiUrl = env.VITE_PUBLIC_API_URL || env.VITE_API_URL || 'http://127.0.0.1:5054';
  
=======
>>>>>>> 0f70b2a (Docker compose touchups)
  return {
    plugins: [react()],
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      css: true,
    },
    server: {
      proxy: {
        '/api': {
<<<<<<< HEAD
          target: apiUrl,
          changeOrigin: true,
        },
        '/hubs': {
          target: apiUrl,
=======
          target: process.env.VITE_PUBLIC_API_URL || 'http://127.0.0.1:8080',
          changeOrigin: true,
        },
        '/hubs': {
          target: process.env.VITE_PUBLIC_API_URL || 'http://127.0.0.1:8080',
>>>>>>> 0f70b2a (Docker compose touchups)
          ws: true,
          changeOrigin: true,
        }
      }
    }
  };
});
