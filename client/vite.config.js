import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The React app calls "/api/...". During development Vite forwards those calls to the .NET API,
// so the browser talks to ONE origin (localhost:5173) and needs no CORS.
//   /api/employees  ->  https://localhost:7080/employees
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'https://localhost:7080',
        changeOrigin: true,
        secure: false, // the .NET dev certificate is self-signed
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
});
