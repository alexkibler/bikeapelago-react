/// <reference types="vitest" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiUrl = env.VITE_PUBLIC_API_URL || env.VITE_API_URL || 'http://127.0.0.1:5054';
  
  return {
    plugins: [react()],
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      css: true,
    },
    server: {
      allowedHosts: ['bikeapelago.alexkibler.com', '.alexkibler.com'],
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
