import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

// Vite plugin: rewrites subdomain requests to their respective HTML entrypoints
const subdomainPlugin = {
    name: 'subdomain-routing',
    configureServer(server) {
        server.middlewares.use((req, _res, next) => {
            const host = req.headers.host ?? ''
            if (req.url === '/') {
                if (host.startsWith('docs.')) req.url = '/docs.html'
                else if (host.startsWith('forum.')) req.url = '/forum.html'
            }
            next()
        })
    },
}

export default defineConfig({
    plugins: [vue(), subdomainPlugin],
    build: {
        rollupOptions: {
            input: {
                main: resolve(__dirname, 'index.html'),
                docs: resolve(__dirname, 'docs.html'),
                forum: resolve(__dirname, 'forum.html'),
            },
        },
    },
    server: {
        port: 5173,
        host: true,
        allowedHosts: ['.ngrok-free.dev', 'docs.localhost', 'forum.localhost'],
        proxy: {
            '/api': {
                target: 'http://localhost:5004',
                changeOrigin: true,
                secure: false,
            },
        },
    },
})