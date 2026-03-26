import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
    plugins: [vue()],
    server: {
        port: 5173,
        host: true,
        allowedHosts: ['.ngrok-free.dev'],
        proxy: {
            '/api': {
                target: 'https://localhost:7114',
                changeOrigin: true,
                secure: false
            }
        }
    }
})