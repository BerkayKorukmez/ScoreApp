import { createApp } from 'vue'
import { createPinia } from 'pinia'
import axios from 'axios'
import router from './router'
import App from './App.vue'
import './style.css'

// Axios base URL (Vite proxy kullanılıyor)
axios.defaults.baseURL = '/api'

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.mount('#app')
