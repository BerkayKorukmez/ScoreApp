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
        <router-link
          :to="homeRoute"
          class="nav-link"
          :class="{ active: isHomePage }"
        >
          <span class="nav-icon">🏟️</span>
          <span class="nav-label">Maçlar</span>
        </router-link>
        <router-link
          :to="pastMatchesRoute"
          class="nav-link"
          :class="{ active: route.path.endsWith('/past-matches') }"
        >
          <span class="nav-icon">📅</span>
          <span class="nav-label">Geçmiş Maçlar</span>
        </router-link>
        <router-link
          :to="fixturesRoute"
          class="nav-link"
          :class="{ active: route.path.endsWith('/fixtures') }"
        >
          <span class="nav-icon">📅</span>
          <span class="nav-label">Fikstür</span>
        </router-link>
        <router-link
          :to="newsRoute"
          class="nav-link"
          :class="{ active: route.path.endsWith('/news') }"
        >
          <span class="nav-icon">📰</span>
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
const showBackButton = computed(() => route.name === 'MatchDetail' || route.name === 'UserMatchDetail')

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
  font-size: 1rem;
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
