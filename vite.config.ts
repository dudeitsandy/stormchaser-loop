import { defineConfig } from 'vite'

export default defineConfig({
  base: './', // CRITICAL for itch.io - use relative paths
  server: { port: 5173 },
  build: {
    target: 'es2020',
    outDir: 'dist',
    assetsDir: 'assets',
    sourcemap: false, // Disable sourcemaps for smaller build
    rollupOptions: {
      output: {
        // Use even simpler naming for itch.io compatibility
        entryFileNames: 'game.js',
        chunkFileNames: 'chunk-[hash].js',
        assetFileNames: 'assets/[name].[ext]'
      }
    }
  }
})

