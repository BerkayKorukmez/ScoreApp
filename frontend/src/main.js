import { createApp } from 'vue'
import { createPinia } from 'pinia'
import axios from 'axios'
import router from './router'
import App from './App.vue'
import './style.css'

// Axios base URL
// - Dev: Vite proxy sayesinde '/api' yeterli
// - Production (Vercel): VITE_API_URL=https://backend.example.com — sonuna '/api' ekleriz
const apiBase = import.meta.env.VITE_API_URL
axios.defaults.baseURL = apiBase ? `${apiBase.replace(/\/$/, '')}/api` : '/api'

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.mount('#app')
