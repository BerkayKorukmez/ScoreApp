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

      <!-- Marka -->
      <router-link :to="homeRoute" class="brand">
        <span class="brand-icon">⚽</span>
        <span class="brand-text">SkorTakip</span>
      </router-link>

      <!-- Ana navigasyon (detay sayfalarında gizlenir) -->
      <nav v-if="!showBackButton" class="header-nav">
        <!-- Maçlar -->
        <router-link :to="homeRoute" class="nav-link" :class="{ active: isHomePage }">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="9"/>
            <path d="M12 3c0 0-2.5 3.5-2.5 5.5s1 3.5 2.5 3.5 2.5-1.5 2.5-3.5S12 3 12 3z"/>
            <path d="M3.6 7.5l3.9 1.2M16.5 8.7l3.9-1.2M5.5 18l2.8-3.2M15.7 14.8l2.8 3.2M8.5 21l.8-3.5M14.7 17.5l.8 3.5"/>
          </svg>
          <span class="nav-label">Maçlar</span>
        </router-link>

        <!-- Geçmiş Maçlar -->
        <router-link :to="pastMatchesRoute" class="nav-link" :class="{ active: route.path.endsWith('/past-matches') }">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 3v5h5"/>
            <path d="M3.05 13A9 9 0 1 0 6 5.3L3 8"/>
            <polyline points="12 7 12 12 16 14"/>
          </svg>
          <span class="nav-label">Geçmiş Maçlar</span>
        </router-link>

        <!-- Fikstür -->
        <router-link :to="fixturesRoute" class="nav-link" :class="{ active: route.path.endsWith('/fixtures') }">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8" y1="2" x2="8" y2="6"/>
            <line x1="3" y1="10" x2="21" y2="10"/>
            <line x1="8" y1="14" x2="8" y2="14"/>
            <line x1="12" y1="14" x2="16" y2="14"/>
            <line x1="8" y1="18" x2="8" y2="18"/>
            <line x1="12" y1="18" x2="16" y2="18"/>
          </svg>
          <span class="nav-label">Fikstür</span>
        </router-link>

        <!-- Haberler -->
        <router-link :to="newsRoute" class="nav-link" :class="{ active: route.path.endsWith('/news') }">
          <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 22h16a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H8a2 2 0 0 0-2 2v16a2 2 0 0 1-2 2zm0 0a2 2 0 0 1-2-2v-9c0-1.1.9-2 2-2h2"/>
            <line x1="10" y1="7" x2="18" y2="7"/>
            <line x1="10" y1="11" x2="18" y2="11"/>
            <line x1="10" y1="15" x2="14" y2="15"/>
          </svg>
          <span class="nav-label">Haberler</span>
        </router-link>
      </nav>
    </div>

    <div class="header-right">
      <template v-if="authStore.isAuthenticated">
        <!-- AI Sohbet (spor asistanı) -->
        <AiChatPanel />
        <!-- Admin paneli linki (sadece admin kullanıcılara) -->
        <router-link v-if="authStore.isAdmin" to="/admin" class="btn-admin">
          ⚡ Admin
        </router-link>
        <div class="user-info">
          <span class="user-avatar">{{ authStore.isAdmin ? '👑' : '👤' }}</span>
          <span class="user-name">{{ authStore.user?.userName || authStore.user?.email || 'Hesabım' }}</span>
        </div>
        <button class="btn-logout" @click="handleLogout">Çıkış</button>
      </template>
      <template v-else>
        <router-link to="/login" class="btn-auth">Giriş Yap</router-link>
        <router-link to="/register" class="btn-auth btn-register">Kayıt Ol</router-link>
      </template>
    </div>
  </header>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import AiChatPanel from './ai/AiChatPanel.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

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

/** Mevcut sayfa ana sayfa mı? (/ veya /user) */
const isHomePage = computed(() => route.path === '/' || route.path === '/user')

/** Çıkış yap */
const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}
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
}

/* ---- Sol Kısım ---- */
.header-left {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  text-decoration: none;
}

.brand-icon {
  font-size: 1.4rem;
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
}

.btn-back:hover {
  background: #21262d;
  border-color: #484f58;
}

.back-icon {
  width: 16px;
  height: 16px;
}

/* ---- Navigasyon ---- */
.header-nav {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  margin-left: 1rem;
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
}

.nav-link:hover {
  color: #c9d1d9;
  background: #21262d;
}

.nav-link.active {
  color: #58a6ff;
  background: #58a6ff15;
}

.nav-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
  color: inherit;
}

/* ---- Sağ Kısım ---- */
.header-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.75rem;
  background: #21262d;
  border-radius: 20px;
}

.user-avatar {
  font-size: 0.85rem;
}

.user-name {
  font-size: 0.8rem;
  font-weight: 500;
  color: #c9d1d9;
}

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
}

.btn-logout:hover {
  background: #f8514922;
  border-color: #f85149;
}

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
}

.btn-auth:hover {
  background: #58a6ff22;
}

.btn-register {
  background: #238636;
  color: #ffffff;
  border-color: #238636;
}

.btn-register:hover {
  background: #2ea043;
}

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
}

.btn-admin:hover {
  background: #3d2880;
  border-color: #6d4fcc;
}

/* ---- Responsive ---- */
@media (max-width: 600px) {
  .top-header {
    padding: 0 1rem;
  }

  .header-nav {
    margin-left: 0.5rem;
  }

  .nav-label {
    display: none;
  }

  .brand-text {
    display: none;
  }

  .user-info {
    display: none;
  }
}
</style>
