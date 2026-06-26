import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    // 500 KB üzeri chunk'lar için uyarı eşiği (varsayılan 500).
    chunkSizeWarningLimit: 600,
    rollupOptions: {
      output: {
        /**
         * Vendor chunk ayrımı: sık değişmeyen bağımlılıklar ayrı chunk'lara
         * taşınarak tarayıcı önbelleği etkinliği artırılır ve ana bundle küçülür.
         */
        manualChunks: (id: string) => {
          if (id.includes('node_modules/react-dom') || id.includes('node_modules/react/')) {
            return 'vendor-react';
          }
          if (id.includes('node_modules/react-router')) {
            return 'vendor-router';
          }
          if (id.includes('node_modules/@tanstack/react-query')) {
            return 'vendor-query';
          }
          if (id.includes('node_modules/@microsoft/signalr')) {
            return 'vendor-signalr';
          }
          // Form yığını (zod + react-hook-form + resolver) ayrı chunk'ta:
          // yalnızca form içeren sayfalarda gerekir; ana giriş bundle'ını incelyip
          // tarayıcı önbelleğini iyileştirir (bu bağımlılıklar nadir değişir).
          if (
            id.includes('node_modules/react-hook-form') ||
            id.includes('node_modules/@hookform') ||
            id.includes('node_modules/zod')
          ) {
            return 'vendor-forms';
          }
        },
      },
    },
  },
})
