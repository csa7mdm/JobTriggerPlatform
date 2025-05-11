import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    // Remove or comment out the proxy to ensure MSW handles API requests in development
    // proxy: {
    //   '/api': {
    //     target: 'https://localhost:7001',
    //     changeOrigin: true,
    //     secure: false,
    //   },
    // },
  },
})
