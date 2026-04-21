<template>
  <div class="auth-page">
    <!-- Sol dekoratif panel -->
    <div class="auth-hero">
      <div class="hero-content">
        <div class="hero-brand">
          <svg class="hero-icon" viewBox="0 0 52 52" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
            <circle cx="26" cy="26" r="25" fill="#2ECC71"/>
            <text x="26" y="36" text-anchor="middle" font-family="'Arial Black', Arial, sans-serif"
                  font-size="28" font-weight="900" fill="#ffffff" letter-spacing="-1">S</text>
          </svg>
          <span class="hero-text">SkorNet</span>
        </div>
        <h2 class="hero-title">Canlı skorları takip et,<br />hiçbir anı kaçırma.</h2>
        <p class="hero-desc">
          Futbol, basketbol ve voleybol maçlarını anlık olarak izle.
          Favori takımlarını ekle, puan tablolarını incele.
        </p>
        <div class="hero-stats">
          <div class="stat-item">
            <span class="stat-number">50+</span>
            <span class="stat-label">Lig</span>
          </div>
          <div class="stat-item">
            <span class="stat-number">1000+</span>
            <span class="stat-label">Maç</span>
          </div>
          <div class="stat-item">
            <span class="stat-number">3</span>
            <span class="stat-label">Spor</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Sağ giriş formu paneli -->
    <div class="auth-form-panel">
      <div class="auth-card">
        <!-- Üst navigasyon -->
        <div class="auth-nav">
          <router-link to="/login" class="auth-nav-link active">Giriş Yap</router-link>
          <router-link to="/register" class="auth-nav-link">Kayıt Ol</router-link>
        </div>

        <!-- Başlık -->
        <div class="auth-header">
          <h1 class="auth-title">Tekrar Hoş Geldin</h1>
          <p class="auth-subtitle">Hesabınla giriş yaparak devam et</p>
        </div>

        <!-- Giriş formu -->
        <form @submit.prevent="handleLogin" class="auth-form">
          <!-- Email alanı -->
          <div class="form-group">
            <label class="form-label">Email</label>
            <div class="input-wrapper" :class="{ focused: emailFocused }">
              <svg class="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/>
                <polyline points="22,6 12,13 2,6"/>
              </svg>
              <input
                v-model="email"
                type="email"
                required
                placeholder="ornek@email.com"
                class="input-field"
                @focus="emailFocused = true"
                @blur="emailFocused = false"
              />
            </div>
          </div>

          <!-- Şifre alanı -->
          <div class="form-group">
            <label class="form-label">Şifre</label>
            <div class="input-wrapper" :class="{ focused: passwordFocused }">
              <svg class="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
              </svg>
              <input
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                required
                placeholder="••••••••"
                class="input-field"
                @focus="passwordFocused = true"
                @blur="passwordFocused = false"
              />
              <button
                type="button"
                @click="showPassword = !showPassword"
                class="password-toggle"
                tabindex="-1"
              >
                <!-- Göz açık ikonu -->
                <svg v-if="showPassword" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
                <!-- Göz kapalı ikonu -->
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                  <line x1="1" y1="1" x2="23" y2="23"/>
                </svg>
              </button>
            </div>
          </div>

          <!-- Hata mesajı -->
          <div v-if="errorMessage" class="error-alert">
            <svg class="error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="15" y1="9" x2="9" y2="15"/>
              <line x1="9" y1="9" x2="15" y2="15"/>
            </svg>
            <span>{{ errorMessage }}</span>
          </div>

          <!-- Giriş butonu -->
          <button type="submit" :disabled="authStore.isLoading" class="btn-submit">
            <span v-if="authStore.isLoading" class="btn-spinner"></span>
            {{ authStore.isLoading ? 'Giriş yapılıyor...' : 'Giriş Yap' }}
          </button>
        </form>

        <!-- Alt bilgi -->
        <div class="auth-footer">
          <p>Hesabınız yok mu? <router-link to="/register">Kayıt Ol</router-link></p>
          <div class="auth-divider">
            <span>veya</span>
          </div>
          <button class="btn-guest" @click="router.push('/')">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="12" cy="8" r="4"/>
              <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/>
            </svg>
            Hesap olmadan devam et
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

/* Durum değişkenleri */
const email = ref('')
const password = ref('')
const showPassword = ref(false)
const errorMessage = ref('')
const emailFocused = ref(false)
const passwordFocused = ref(false)

/* Giriş işlemi */
const handleLogin = async () => {
  errorMessage.value = ''
  const result = await authStore.login(email.value, password.value)

  if (result.success) {
    router.push('/user')
  } else {
    errorMessage.value = result.message
  }
}
</script>

<style scoped>
/* =============================================
   SAYFA KONTEYNER
   ============================================= */
.auth-page {
  display: flex;
  min-height: 100vh;
  background: #0e1118;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

/* =============================================
   SOL DEKORATİF PANEL (HERO)
   ============================================= */
.auth-hero {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d1117 0%, #161b22 50%, #0d1117 100%);
  position: relative;
  overflow: hidden;
  padding: 3rem;
}

/* Arka plan dekoratif daireler */
.auth-hero::before {
  content: '';
  position: absolute;
  top: -15%;
  left: -10%;
  width: 500px;
  height: 500px;
  border-radius: 50%;
  background: radial-gradient(circle, #58a6ff11 0%, transparent 70%);
}

.auth-hero::after {
  content: '';
  position: absolute;
  bottom: -10%;
  right: -5%;
  width: 400px;
  height: 400px;
  border-radius: 50%;
  background: radial-gradient(circle, #27AE6015 0%, transparent 70%);
}

.hero-content {
  position: relative;
  z-index: 1;
  max-width: 420px;
}

.hero-brand {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 2rem;
}

.hero-icon {
  width: 44px;
  height: 44px;
  flex-shrink: 0;
  display: block;
}

.hero-text {
  font-size: 1.5rem;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: -0.5px;
}

.hero-title {
  font-size: 2.2rem;
  font-weight: 700;
  color: #ffffff;
  line-height: 1.3;
  margin-bottom: 1rem;
  letter-spacing: -0.5px;
}

.hero-desc {
  font-size: 0.95rem;
  color: #8b949e;
  line-height: 1.7;
  margin-bottom: 2.5rem;
}

.hero-stats {
  display: flex;
  gap: 2.5rem;
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.stat-number {
  font-size: 1.6rem;
  font-weight: 700;
  color: #58a6ff;
}

.stat-label {
  font-size: 0.75rem;
  color: #484f58;
  text-transform: uppercase;
  letter-spacing: 1px;
  font-weight: 600;
}

/* =============================================
   SAĞ FORM PANELİ
   ============================================= */
.auth-form-panel {
  width: 480px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  background: #0d1117;
  border-left: 1px solid #21262d;
}

.auth-card {
  width: 100%;
  max-width: 380px;
}

/* =============================================
   ÜST NAVİGASYON SEKMELERİ
   ============================================= */
.auth-nav {
  display: flex;
  gap: 0;
  margin-bottom: 2rem;
  border-bottom: 1px solid #21262d;
}

.auth-nav-link {
  flex: 1;
  text-align: center;
  padding: 0.75rem 0;
  font-size: 0.85rem;
  font-weight: 500;
  color: #8b949e;
  text-decoration: none;
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
}

.auth-nav-link:hover {
  color: #c9d1d9;
}

.auth-nav-link.active {
  color: #58a6ff;
  border-bottom-color: #58a6ff;
}

/* =============================================
   BAŞLIK
   ============================================= */
.auth-header {
  margin-bottom: 2rem;
}

.auth-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #ffffff;
  margin: 0 0 0.4rem 0;
}

.auth-subtitle {
  font-size: 0.85rem;
  color: #484f58;
  margin: 0;
}

/* =============================================
   FORM
   ============================================= */
.auth-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.form-label {
  font-size: 0.8rem;
  font-weight: 500;
  color: #c9d1d9;
}

.input-wrapper {
  display: flex;
  align-items: center;
  background: #0d1117;
  border: 1px solid #30363d;
  border-radius: 8px;
  padding: 0 0.85rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.input-wrapper.focused {
  border-color: #58a6ff;
  box-shadow: 0 0 0 3px #58a6ff22;
}

.input-icon {
  width: 18px;
  height: 18px;
  color: #484f58;
  flex-shrink: 0;
  margin-right: 0.65rem;
}

.input-wrapper.focused .input-icon {
  color: #58a6ff;
}

.input-field {
  flex: 1;
  background: transparent;
  border: none;
  color: #e1e4e8;
  font-size: 0.9rem;
  padding: 0.75rem 0;
  outline: none;
  font-family: inherit;
}

.input-field::placeholder {
  color: #30363d;
}

.password-toggle {
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.25rem;
  margin-left: 0.35rem;
  display: flex;
  align-items: center;
  color: #484f58;
  transition: color 0.2s;
}

.password-toggle:hover {
  color: #8b949e;
}

.password-toggle svg {
  width: 18px;
  height: 18px;
}

/* =============================================
   HATA MESAJI
   ============================================= */
.error-alert {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #f8514922;
  border: 1px solid #f8514944;
  color: #f85149;
  padding: 0.65rem 0.85rem;
  border-radius: 8px;
  font-size: 0.8rem;
  font-weight: 500;
}

.error-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

/* =============================================
   GÖNDERİM BUTONU
   ============================================= */
.btn-submit {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.8rem;
  background: #27AE60;
  color: #ffffff;
  border: 1px solid #27AE60;
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-family: inherit;
  margin-top: 0.5rem;
}

.btn-submit:hover:not(:disabled) {
  background: #27AE60;
  border-color: #27AE60;
}

.btn-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid #ffffff44;
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* =============================================
   ALT BİLGİ
   ============================================= */
.auth-footer {
  text-align: center;
  margin-top: 1.75rem;
  padding-top: 1.25rem;
  border-top: 1px solid #21262d;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.auth-footer p {
  font-size: 0.8rem;
  color: #484f58;
  margin: 0;
}

.auth-footer a {
  color: #58a6ff;
  text-decoration: none;
  font-weight: 600;
}

.auth-footer a:hover {
  text-decoration: underline;
}

.auth-divider {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: #30363d;
  font-size: 0.75rem;
}

.auth-divider::before,
.auth-divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: #21262d;
}

.btn-guest {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.7rem;
  background: transparent;
  color: #8b949e;
  border: 1px solid #30363d;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  font-family: inherit;
}

.btn-guest svg {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.btn-guest:hover {
  background: #161b22;
  color: #c9d1d9;
  border-color: #484f58;
}

/* =============================================
   MOBİL UYUMLULUK
   ============================================= */
@media (max-width: 900px) {
  .auth-page {
    flex-direction: column;
  }

  .auth-hero {
    padding: 2rem 1.5rem;
    min-height: auto;
  }

  .hero-title {
    font-size: 1.6rem;
  }

  .hero-desc {
    display: none;
  }

  .hero-stats {
    display: none;
  }

  .auth-form-panel {
    width: 100%;
    border-left: none;
    border-top: 1px solid #21262d;
  }
}

@media (max-width: 500px) {
  .auth-hero {
    padding: 1.5rem 1rem;
  }

  .hero-title {
    font-size: 1.3rem;
  }

  .auth-form-panel {
    padding: 1.5rem 1rem;
  }
}
</style>
