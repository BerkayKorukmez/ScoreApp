import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'

export const useAuthStore = defineStore('auth', () => {
  // localStorage'dan kullanıcı ve token bilgisini geri yükle
  const storedUser = localStorage.getItem('user')
  const user = ref(storedUser ? JSON.parse(storedUser) : null)
  const token = ref(localStorage.getItem('token') || null)
  const isLoading = ref(false)

  const isAuthenticated = computed(() => !!token.value)

  // Axios interceptor - sadece bir kez kurulur
  let interceptorSetup = false
  const setupAxiosInterceptor = () => {
    if (interceptorSetup) return
    interceptorSetup = true

    axios.interceptors.request.use(
      (config) => {
        if (token.value) {
          config.headers.Authorization = `Bearer ${token.value}`
        }
        return config
      },
      (error) => Promise.reject(error)
    )

    axios.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          logout()
        }
        return Promise.reject(error)
      }
    )
  }

  // Uygulama başlar başlamaz interceptor'ı kur
  setupAxiosInterceptor()

  const login = async (email, password) => {
    isLoading.value = true
    try {
      const response = await axios.post('http://localhost:5000/api/auth/login', {
        email,
        password
      })
      
      token.value = response.data.token
      user.value = {
        id: response.data.userId,
        email: response.data.email,
        userName: response.data.userName
      }
      localStorage.setItem('token', token.value)
      localStorage.setItem('user', JSON.stringify(user.value))
      
      return { success: true }
    } catch (error) {
      return {
        success: false,
        message: error.response?.data?.message || error.response?.data?.errors?.[0] || 'Giriş yapılamadı'
      }
    } finally {
      isLoading.value = false
    }
  }

  const register = async (username, email, password, firstName = '', lastName = '') => {
    isLoading.value = true
    try {
      const response = await axios.post('http://localhost:5000/api/auth/register', {
        email,
        password,
        firstName: firstName || username,
        lastName: lastName || username,
        userName: username
      })
      
      token.value = response.data.token
      user.value = {
        id: response.data.userId,
        email: response.data.email,
        userName: response.data.userName
      }
      localStorage.setItem('token', token.value)
      localStorage.setItem('user', JSON.stringify(user.value))
      
      return { success: true }
    } catch (error) {
      return {
        success: false,
        message: error.response?.data?.message || error.response?.data?.errors?.[0] || 'Kayıt olunamadı'
      }
    } finally {
      isLoading.value = false
    }
  }

  const logout = () => {
    token.value = null
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  const fetchCurrentUser = async () => {
    if (!token.value) return

    try {
      const response = await axios.get('http://localhost:5000/api/auth/me')
      user.value = response.data
      return { success: true }
    } catch (error) {
      logout()
      return { success: false }
    }
  }

  return {
    user,
    token,
    isLoading,
    isAuthenticated,
    login,
    register,
    logout,
    fetchCurrentUser
  }
})
