import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

// Em desenvolvimento, o Vite roda na porta 5173 e repassa /api e /fotos para a API .NET.
// No build, o site vai para a pasta wwwroot da API, que passa a servir tudo junto.
const api = 'http://localhost:5219'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    proxy: {
      '/api': api,
      '/fotos': api,
    },
  },
  build: {
    outDir: '../Backend/src/Clinica.Api/wwwroot',
    emptyOutDir: true,
  },
})
