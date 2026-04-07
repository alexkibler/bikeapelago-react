export default {
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5054',
        changeOrigin: true,
      }
    }
  }
}