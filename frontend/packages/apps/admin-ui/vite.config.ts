import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// https://vite.dev/config/
<<<<<<< HEAD
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiUrl = env.VITE_PUBLIC_API_URL || env.VITE_API_URL || 'http://127.0.0.1:8080';

  return {
    plugins: [
      react(),
      tailwindcss(),
    ],
    server: {
      proxy: {
        '/api': {
          target: apiUrl,
          changeOrigin: true,
        },
=======
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    proxy: {
      '/api': {
        target: process.env.VITE_PUBLIC_API_URL || 'http://127.0.0.1:8080',
        changeOrigin: true,
>>>>>>> 0f70b2a (Docker compose touchups)
      },
    },
  };
});
