import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'

export const useAuthStore = defineStore('auth', () => {
  // localStorage'dan kullanıcı ve token bilgisini geri yükle
  const storedUser = localStorage.getItem('user')
  const user  = ref(storedUser ? JSON.parse(storedUser) : null)
  const token = ref(localStorage.getItem('token') || null)
  const isLoading = ref(false)

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin         = computed(() => !!user.value?.isAdmin)

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

  setupAxiosInterceptor()

  const _saveSession = (data) => {
    token.value = data.token
    user.value = {
      id:       data.userId,
      email:    data.email,
      userName: data.userName,
      isAdmin:  data.isAdmin ?? false
    }
    localStorage.setItem('token', token.value)
    localStorage.setItem('user', JSON.stringify(user.value))
  }

  const login = async (email, password) => {
    isLoading.value = true
    try {
      const response = await axios.post('/auth/login', { email, password })
      _saveSession(response.data)
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
      const response = await axios.post('/auth/register', {
        email,
        password,
        firstName: firstName || username,
        lastName:  lastName  || username,
        userName:  username
      })
      _saveSession(response.data)
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
    user.value  = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  const fetchCurrentUser = async () => {
    if (!token.value) return
    try {
      const response = await axios.get('/auth/me')
      user.value = {
        ...user.value,
        ...response.data,
        isAdmin: response.data.isAdmin ?? false
      }
      localStorage.setItem('user', JSON.stringify(user.value))
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
    isAdmin,
    login,
    register,
    logout,
    fetchCurrentUser
  }
})
