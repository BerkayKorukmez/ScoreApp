<template>
  <div class="auth-page">
    <!-- Sol dekoratif panel -->
    <div class="auth-hero">
      <div class="hero-content">
        <div class="hero-brand">
          <span class="hero-icon">⚽</span>
          <span class="hero-text">SkorTakip</span>
        </div>
        <h2 class="hero-title">Favori takımlarını kaydet,<br />anlık bildirimler al.</h2>
        <p class="hero-desc">
          Ücretsiz hesap oluştur ve maçları favori listene ekle.
          Canlı skor güncellemelerini anında takip et.
        </p>
        <div class="hero-features">
          <div class="feature-item">
            <span class="feature-icon">⭐</span>
            <span class="feature-text">Favori maçları kaydet</span>
          </div>
          <div class="feature-item">
            <span class="feature-icon">📊</span>
            <span class="feature-text">Puan tablolarını incele</span>
          </div>
          <div class="feature-item">
            <span class="feature-icon">🔴</span>
            <span class="feature-text">Canlı skor takibi</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Sağ kayıt formu paneli -->
    <div class="auth-form-panel">
      <div class="auth-card">
        <!-- Üst navigasyon -->
        <div class="auth-nav">
          <router-link to="/login" class="auth-nav-link">Giriş Yap</router-link>
          <router-link to="/register" class="auth-nav-link active">Kayıt Ol</router-link>
        </div>

        <!-- Başlık -->
        <div class="auth-header">
          <h1 class="auth-title">Hesap Oluştur</h1>
          <p class="auth-subtitle">Birkaç saniyede kayıt ol ve takibe başla</p>
        </div>

        <!-- Kayıt formu -->
        <form @submit.prevent="handleRegister" class="auth-form">
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
                placeholder="En az 6 karakter"
                class="input-field"
                minlength="6"
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
            <!-- Şifre gücü göstergesi -->
            <div class="password-strength">
              <div class="strength-bar">
                <div class="strength-fill" :style="{ width: passwordStrength + '%' }" :class="strengthClass"></div>
              </div>
              <span class="strength-text" :class="strengthClass">{{ strengthLabel }}</span>
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

          <!-- Kayıt butonu -->
          <button type="submit" :disabled="authStore.isLoading" class="btn-submit">
            <span v-if="authStore.isLoading" class="btn-spinner"></span>
            {{ authStore.isLoading ? 'Kayıt yapılıyor...' : 'Hesap Oluştur' }}
          </button>
        </form>

        <!-- Alt bilgi -->
        <div class="auth-footer">
          <p>Zaten bir hesabınız var mı? <router-link to="/login">Giriş Yap</router-link></p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
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

/* Şifre gücü hesaplayıcı (kullanıcıya görsel geri bildirim için) */
const passwordStrength = computed(() => {
  const p = password.value
  if (!p) return 0
  let score = 0
  if (p.length >= 6) score += 25
  if (p.length >= 10) score += 15
  if (/[A-Z]/.test(p)) score += 20
  if (/[0-9]/.test(p)) score += 20
  if (/[^A-Za-z0-9]/.test(p)) score += 20
  return Math.min(score, 100)
})

const strengthClass = computed(() => {
  if (passwordStrength.value <= 25) return 'weak'
  if (passwordStrength.value <= 50) return 'fair'
  if (passwordStrength.value <= 75) return 'good'
  return 'strong'
})

const strengthLabel = computed(() => {
  if (!password.value) return ''
  if (passwordStrength.value <= 25) return 'Zayıf'
  if (passwordStrength.value <= 50) return 'Orta'
  if (passwordStrength.value <= 75) return 'İyi'
  return 'Güçlü'
})

/* Kayıt işlemi */
const handleRegister = async () => {
  errorMessage.value = ''
  // Email'den otomatik kullanıcı adı oluştur
  const username = email.value.split('@')[0]
  const result = await authStore.register(username, email.value, password.value)

  if (result.success) {
    router.push('/')
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
  background: radial-gradient(circle, #23863615 0%, transparent 70%);
}

.auth-hero::after {
  content: '';
  position: absolute;
  bottom: -10%;
  right: -5%;
  width: 400px;
  height: 400px;
  border-radius: 50%;
  background: radial-gradient(circle, #58a6ff11 0%, transparent 70%);
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
  font-size: 2rem;
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

/* Özellik listesi (kayıt sayfası) */
.hero-features {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.feature-icon {
  font-size: 1.1rem;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #21262d;
  border-radius: 8px;
}

.feature-text {
  font-size: 0.9rem;
  color: #c9d1d9;
  font-weight: 500;
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
   ŞİFRE GÜÇ GÖSTERGESİ
   ============================================= */
.password-strength {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 0.3rem;
}

.strength-bar {
  flex: 1;
  height: 3px;
  background: #21262d;
  border-radius: 2px;
  overflow: hidden;
}

.strength-fill {
  height: 100%;
  border-radius: 2px;
  transition: width 0.3s, background 0.3s;
}

.strength-fill.weak {
  background: #f85149;
}

.strength-fill.fair {
  background: #d29922;
}

.strength-fill.good {
  background: #58a6ff;
}

.strength-fill.strong {
  background: #3fb950;
}

.strength-text {
  font-size: 0.7rem;
  font-weight: 500;
  min-width: 35px;
}

.strength-text.weak {
  color: #f85149;
}

.strength-text.fair {
  color: #d29922;
}

.strength-text.good {
  color: #58a6ff;
}

.strength-text.strong {
  color: #3fb950;
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
  background: #238636;
  color: #ffffff;
  border: 1px solid #238636;
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-family: inherit;
  margin-top: 0.5rem;
}

.btn-submit:hover:not(:disabled) {
  background: #2ea043;
  border-color: #2ea043;
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

  .hero-features {
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
