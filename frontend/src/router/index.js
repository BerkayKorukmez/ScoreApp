import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  // ── Public routes (giriş gerektirmez) ────────────────────────────────────────
  {
    path: '/',
    component: () => import('../layouts/MainLayout.vue'),
    children: [
      {
        path: '',
        name: 'Home',
        component: () => import('../views/Home.vue')
      },
      {
        path: 'news',
        name: 'News',
        component: () => import('../views/News.vue')
      },
      {
        path: 'past-matches',
        name: 'PastMatches',
        component: () => import('../views/PastMatches.vue')
      },
      {
        path: 'fixtures',
        name: 'Fixtures',
        component: () => import('../views/Fixtures.vue')
      },
      {
        path: 'match/:id',
        name: 'MatchDetail',
        component: () => import('../views/MatchDetail.vue'),
        props: true
      },
      {
        path: 'team/:id',
        name: 'TeamProfile',
        component: () => import('../views/TeamProfile.vue'),
        props: true
      },
      {
        path: 'player/:id',
        name: 'PlayerProfile',
        component: () => import('../views/PlayerProfile.vue'),
        props: true
      }
    ]
  },

  // ── Authenticated user routes (giriş gerektirir) ─────────────────────────────
  {
    path: '/user',
    component: () => import('../layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'UserHome',
        component: () => import('../views/Home.vue')
      },
      {
        path: 'news',
        name: 'UserNews',
        component: () => import('../views/News.vue')
      },
      {
        path: 'past-matches',
        name: 'UserPastMatches',
        component: () => import('../views/PastMatches.vue')
      },
      {
        path: 'fixtures',
        name: 'UserFixtures',
        component: () => import('../views/Fixtures.vue')
      },
      {
        path: 'match/:id',
        name: 'UserMatchDetail',
        component: () => import('../views/MatchDetail.vue'),
        props: true
      },
      {
        path: 'team/:id',
        name: 'UserTeamProfile',
        component: () => import('../views/TeamProfile.vue'),
        props: true
      },
      {
        path: 'player/:id',
        name: 'UserPlayerProfile',
        component: () => import('../views/PlayerProfile.vue'),
        props: true
      }
    ]
  },

  // ── Admin paneli (requiresAdmin) ─────────────────────────────────────────────
  {
    path: '/admin',
    name: 'Admin',
    component: () => import('../views/Admin.vue'),
    meta: { requiresAdmin: true }
  },

  // ── Auth sayfaları ────────────────────────────────────────────────────────────
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue'),
    meta: { requiresGuest: true }
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('../views/Register.vue'),
    meta: { requiresGuest: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const isAuth  = authStore.isAuthenticated
  const isAdmin = authStore.isAdmin

  // /admin → giriş yapmamış veya admin değil
  if (to.meta.requiresAdmin) {
    if (!isAuth)  { next({ name: 'Login' }); return }
    if (!isAdmin) { next({ name: isAuth ? 'UserHome' : 'Home' }); return }
  }

  // /user/* → giriş gerekmez → public Home'a yönlendir
  if (to.meta.requiresAuth && !isAuth) {
    next({ path: '/' })
    return
  }

  // Login/Register sayfası → zaten giriş yapılmış → /user'a yönlendir
  if (to.meta.requiresGuest && isAuth) {
    next({ name: 'UserHome' })
    return
  }

  // / (public home) → giriş yapılmış → /user'a yönlendir
  if (to.name === 'Home' && isAuth) {
    next({ name: 'UserHome' })
    return
  }

  next()
})

export default router
