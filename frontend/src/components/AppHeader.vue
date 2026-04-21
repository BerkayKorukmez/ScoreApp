<template>
  <header class="top-header">
    <div class="header-left">
      <!-- Geri butonu (detay sayfalarında gösterilir) -->
      <button v-if="showBackButton" class="btn-back" @click="router.go(-1)">
        <svg class="back-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="15 18 9 12 15 6"/>
        </svg>
        <span>Geri</span>
      </button>

      <!-- Mobil hamburger (yalnızca ana navlar görünecekse) -->
      <button
        v-if="!showBackButton"
        class="btn-hamburger"
        :aria-expanded="mobileMenuOpen"
        aria-label="Menüyü aç/kapa"
        @click="mobileMenuOpen = !mobileMenuOpen"
      >
        <svg v-if="!mobileMenuOpen" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
          <line x1="3" y1="6" x2="21" y2="6"/>
          <line x1="3" y1="12" x2="21" y2="12"/>
          <line x1="3" y1="18" x2="21" y2="18"/>
        </svg>
        <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
          <line x1="6" y1="6" x2="18" y2="18"/>
          <line x1="6" y1="18" x2="18" y2="6"/>
        </svg>
      </button>

      <!-- Marka -->
      <router-link :to="homeRoute" class="brand" @click="mobileMenuOpen = false">
        <svg class="brand-icon" viewBox="0 0 36 36" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
          <!-- Yeşil yuvarlak badge -->
          <circle cx="18" cy="18" r="17" fill="#2ECC71"/>
          <!-- Beyaz "S" harfi — bold, modern -->
          <text x="18" y="24.5" text-anchor="middle" font-family="'Arial Black', Arial, sans-serif"
                font-size="19" font-weight="900" fill="#ffffff" letter-spacing="-1">S</text>
        </svg>
        <span class="brand-text">SkorNet</span>
      </router-link>

      <!-- Ana navigasyon (detay sayfalarında gizlenir) -->
      <nav v-if="!showBackButton" class="header-nav" :class="{ open: mobileMenuOpen }">
        <router-link :to="homeRoute" class="nav-link" :class="{ active: isHomePage }" @click="mobileMenuOpen = false">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="9"/>
            <path d="M12 3c0 0-2.5 3.5-2.5 5.5s1 3.5 2.5 3.5 2.5-1.5 2.5-3.5S12 3 12 3z"/>
            <path d="M3.6 7.5l3.9 1.2M16.5 8.7l3.9-1.2M5.5 18l2.8-3.2M15.7 14.8l2.8 3.2M8.5 21l.8-3.5M14.7 17.5l.8 3.5"/>
          </svg>
          <span class="nav-label">Maçlar</span>
        </router-link>

        <router-link :to="pastMatchesRoute" class="nav-link" :class="{ active: route.path.endsWith('/past-matches') }" @click="mobileMenuOpen = false">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 3v5h5"/>
            <path d="M3.05 13A9 9 0 1 0 6 5.3L3 8"/>
            <polyline points="12 7 12 12 16 14"/>
          </svg>
          <span class="nav-label">Geçmiş Maçlar</span>
        </router-link>

        <router-link :to="fixturesRoute" class="nav-link" :class="{ active: route.path.endsWith('/fixtures') }" @click="mobileMenuOpen = false">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8" y1="2" x2="8" y2="6"/>
            <line x1="3" y1="10" x2="21" y2="10"/>
          </svg>
          <span class="nav-label">Fikstür</span>
        </router-link>

        <router-link :to="newsRoute" class="nav-link" :class="{ active: route.path.endsWith('/news') }" @click="mobileMenuOpen = false">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 22h16a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H8a2 2 0 0 0-2 2v16a2 2 0 0 1-2 2zm0 0a2 2 0 0 1-2-2v-9c0-1.1.9-2 2-2h2"/>
            <line x1="10" y1="7" x2="18" y2="7"/>
            <line x1="10" y1="11" x2="18" y2="11"/>
            <line x1="10" y1="15" x2="14" y2="15"/>
          </svg>
          <span class="nav-label">Haberler</span>
        </router-link>

        <!-- Mobil menüde auth eylemleri de burada görünsün -->
        <div class="mobile-auth-actions">
          <template v-if="authStore.isAuthenticated">
            <router-link v-if="authStore.isAdmin" to="/admin" class="btn-admin" @click="mobileMenuOpen = false">⚡ Admin</router-link>
            <div class="user-info">
              <span class="user-avatar">{{ authStore.isAdmin ? '👑' : '👤' }}</span>
              <span class="user-name">{{ authStore.user?.userName || authStore.user?.email || 'Hesabım' }}</span>
            </div>
            <button class="btn-change-pw" @click="showChangePassword = true; mobileMenuOpen = false">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
              </svg>
              Şifre Değiştir
            </button>
            <button class="btn-logout" @click="handleLogout">Çıkış</button>
          </template>
          <template v-else>
            <router-link to="/login" class="btn-auth" @click="mobileMenuOpen = false">Giriş Yap</router-link>
            <router-link to="/register" class="btn-auth btn-register" @click="mobileMenuOpen = false">Kayıt Ol</router-link>
          </template>
        </div>
      </nav>
    </div>

    <div class="header-right">
      <template v-if="authStore.isAuthenticated">
        <AiChatPanel />
        <router-link v-if="authStore.isAdmin" to="/admin" class="btn-admin desktop-only">⚡ Admin</router-link>
        <div class="user-info desktop-only">
          <span class="user-avatar">{{ authStore.isAdmin ? '👑' : '👤' }}</span>
          <span class="user-name">{{ authStore.user?.userName || authStore.user?.email || 'Hesabım' }}</span>
        </div>
        <button class="btn-change-pw desktop-only" @click="showChangePassword = true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
          </svg>
        </button>
        <button class="btn-logout desktop-only" @click="handleLogout">Çıkış</button>
      </template>
      <template v-else>
        <router-link to="/login" class="btn-auth desktop-only">Giriş Yap</router-link>
        <router-link to="/register" class="btn-auth btn-register desktop-only">Kayıt Ol</router-link>
      </template>
    </div>
  </header>

  <!-- Mobil menü açıkken arkaya overlay koy -->
  <div
    v-if="mobileMenuOpen && !showBackButton"
    class="mobile-menu-backdrop"
    @click="mobileMenuOpen = false"
  ></div>

  <!-- Şifre Değiştir Modal -->
  <ChangePasswordModal
    v-model="showChangePassword"
    @success="onPasswordChanged"
  />
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import AiChatPanel from './ai/AiChatPanel.vue'
import ChangePasswordModal from './ChangePasswordModal.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const mobileMenuOpen     = ref(false)
const showChangePassword = ref(false)

/** Detay sayfalarında geri butonu göster */
const showBackButton = computed(() => [
  'MatchDetail', 'UserMatchDetail',
  'TeamProfile', 'UserTeamProfile',
  'PlayerProfile', 'UserPlayerProfile'
].includes(route.name))

/** Auth state'e göre dinamik rotalar */
const homeRoute        = computed(() => authStore.isAuthenticated ? '/user'              : '/')
const pastMatchesRoute = computed(() => authStore.isAuthenticated ? '/user/past-matches' : '/past-matches')
const fixturesRoute    = computed(() => authStore.isAuthenticated ? '/user/fixtures'     : '/fixtures')
const newsRoute        = computed(() => authStore.isAuthenticated ? '/user/news'         : '/news')

const isHomePage = computed(() => route.path === '/' || route.path === '/user')

// Rota değişince mobil menüyü kapat
watch(() => route.fullPath, () => { mobileMenuOpen.value = false })

const handleLogout = () => {
  mobileMenuOpen.value = false
  authStore.logout()
  router.push('/login')
}

const onPasswordChanged = () => { /* başarı toast'u modal içinde gösteriliyor */ }
</script>

<style scoped>
.top-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1.5rem;
  height: 56px;
  background: #161b22;
  border-bottom: 1px solid #21262d;
  position: sticky;
  top: 0;
  z-index: 100;
  gap: 0.5rem;
}

/* ---- Sol Kısım ---- */
.header-left {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  flex: 1;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  text-decoration: none;
  flex-shrink: 0;
}

.brand-icon {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  display: block;
  overflow: visible;
}
.brand-text {
  font-size: 1.2rem;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: -0.5px;
}

/* Geri butonu */
.btn-back {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  background: transparent;
  border: 1px solid #30363d;
  color: #c9d1d9;
  padding: 0.35rem 0.75rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;
}
.btn-back:hover { background: #21262d; border-color: #484f58; }
.back-icon { width: 16px; height: 16px; }

/* Hamburger */
.btn-hamburger {
  display: none;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  background: transparent;
  border: 1px solid #30363d;
  border-radius: 6px;
  color: #c9d1d9;
  cursor: pointer;
  flex-shrink: 0;
}
.btn-hamburger:hover { background: #21262d; }

/* ---- Navigasyon ---- */
.header-nav {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  margin-left: 1rem;
  min-width: 0;
  flex-wrap: wrap;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.35rem 0.85rem;
  color: #8b949e;
  text-decoration: none;
  font-size: 0.82rem;
  font-weight: 500;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}
.nav-link:hover { color: #c9d1d9; background: #21262d; }
.nav-link.active { color: #58a6ff; background: #58a6ff15; }

.nav-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
  color: inherit;
}

.mobile-auth-actions { display: none; }

/* ---- Sağ Kısım ---- */
.header-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-shrink: 0;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.75rem;
  background: #21262d;
  border-radius: 20px;
  max-width: 180px;
}

.user-avatar { font-size: 0.85rem; }

.user-name {
  font-size: 0.8rem;
  font-weight: 500;
  color: #c9d1d9;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.btn-change-pw {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  background: transparent;
  color: #8b949e;
  border: 1px solid #30363d;
  padding: 0.35rem 0.75rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}
.btn-change-pw svg { width: 15px; height: 15px; }
.btn-change-pw:hover { color: #c9d1d9; background: #21262d; border-color: #484f58; }

.btn-logout {
  background: transparent;
  color: #f85149;
  border: 1px solid #f8514933;
  padding: 0.35rem 0.85rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}
.btn-logout:hover { background: #f8514922; border-color: #f85149; }

.btn-auth {
  background: transparent;
  color: #58a6ff;
  border: 1px solid #58a6ff33;
  padding: 0.35rem 0.85rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  text-decoration: none;
  transition: all 0.2s;
  white-space: nowrap;
}
.btn-auth:hover { background: #58a6ff22; }

.btn-register { background: #27AE60; color: #ffffff; border-color: #27AE60; }
.btn-register:hover { background: #27AE60; }

.btn-admin {
  display: flex;
  align-items: center;
  gap: 4px;
  background: #2d2060;
  color: #a78bfa;
  border: 1px solid #4c3aaa;
  padding: 0.35rem 0.85rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.2s;
  white-space: nowrap;
}
.btn-admin:hover { background: #3d2880; border-color: #6d4fcc; }

.mobile-menu-backdrop {
  display: none;
  position: fixed;
  inset: 56px 0 0 0;
  background: rgba(0,0,0,0.5);
  z-index: 90;
}

/* ---- Responsive breakpoints ---- */

/* Orta cihazlar — nav linkleri biraz daralsın */
@media (max-width: 1024px) {
  .top-header { padding: 0 1rem; }
  .header-nav { margin-left: 0.5rem; gap: 0; }
  .nav-link { padding: 0.35rem 0.6rem; font-size: 0.78rem; }
  .user-name { max-width: 100px; }
}

/* Mobil — hamburger menü devreye girer */
@media (max-width: 768px) {
  .top-header { padding: 0 0.85rem; height: 54px; }

  .btn-hamburger { display: inline-flex; }

  /* Masaüstü navigasyonu panel'e dönüşsün */
  .header-nav {
    display: none;
    position: fixed;
    top: 54px;
    left: 0;
    right: 0;
    background: #0f141a;
    border-bottom: 1px solid #21262d;
    padding: 0.75rem 1rem;
    flex-direction: column;
    align-items: stretch;
    gap: 0.25rem;
    max-height: calc(100vh - 54px);
    overflow-y: auto;
    z-index: 95;
    box-shadow: 0 12px 24px rgba(0,0,0,0.35);
  }

  .header-nav.open { display: flex; }

  .nav-link {
    padding: 0.75rem 1rem;
    font-size: 0.95rem;
    border-radius: 8px;
  }
  .nav-link .nav-label { display: inline !important; }

  .mobile-auth-actions {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-top: 0.75rem;
    padding-top: 0.75rem;
    border-top: 1px solid #21262d;
  }
  .mobile-auth-actions .btn-auth,
  .mobile-auth-actions .btn-logout,
  .mobile-auth-actions .btn-admin,
  .mobile-auth-actions .btn-change-pw {
    width: 100%;
    justify-content: center;
    padding: 0.6rem 1rem;
    font-size: 0.9rem;
  }

  .mobile-menu-backdrop { display: block; }

  .desktop-only { display: none !important; }

  .brand-text { display: none; }
}

@media (max-width: 420px) {
  .top-header { padding: 0 0.5rem; gap: 0.25rem; }
}
</style>
