import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import { VitePWA } from 'vite-plugin-pwa'

const API = 'http://localhost:5080'

export default defineConfig({
  plugins: [
    vue(),
    vuetify({ autoImport: true }),

    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg'],
      manifest: {
        name: 'Monitor de Enchentes',
        short_name: 'Enchentes',
        description: 'Registro colaborativo de enchentes e mapa das áreas de risco.',
        lang: 'pt-BR',
        theme_color: '#796C86',
        background_color: '#FBF8FC',
        display: 'standalone',
        start_url: '/',
        icons: [
          { src: 'icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' }
        ]
      },
      workbox: {
        navigateFallbackDenylist: [/^\/api\//, /^\/uploads\//, /^\/openapi\//]
      }
    })
  ],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
  },
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/api': API,
      '/uploads': API
    }
  },
  build: {
    outDir: '../FloodMonitor.Server/wwwroot',
    emptyOutDir: true
  }
})
