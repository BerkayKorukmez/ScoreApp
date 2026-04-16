<template>
  <div class="profile-page">
    <LoadingSpinner v-if="isLoading" label="Kulüp yükleniyor..." />

    <div v-else-if="error" class="error-state">
      <span class="error-icon">⚠️</span>
      <p>{{ error }}</p>
    </div>

    <template v-else-if="team">
      <!-- HEADER -->
      <div class="team-hero">
        <img
          v-if="team.logo"
          :src="team.logo"
          :alt="team.name"
          class="team-hero-logo"
          @error="$event.target.style.display='none'"
        />
        <div class="team-hero-info">
          <h1 class="team-hero-name">{{ team.name }}</h1>
          <div class="team-hero-meta">
            <span v-if="team.country" class="meta-chip">🌍 {{ team.country }}</span>
            <span v-if="team.founded" class="meta-chip">📅 Kuruluş: {{ team.founded }}</span>
            <span v-if="team.national" class="meta-chip meta-chip--special">🏳️ Milli Takım</span>
          </div>
        </div>
      </div>

      <!-- STADYUM -->
      <section v-if="team.venue" class="profile-section">
        <h2 class="section-title">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="section-icon">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
            <polyline points="9 22 9 12 15 12 15 22"/>
          </svg>
          Stadyum
        </h2>
        <div class="venue-card">
          <img
            v-if="team.venue.image"
            :src="team.venue.image"
            :alt="team.venue.name"
            class="venue-image"
            @error="$event.target.style.display='none'"
          />
          <div class="venue-info">
            <p class="venue-name">{{ team.venue.name }}</p>
            <div class="venue-details">
              <span v-if="team.venue.city">📍 {{ team.venue.city }}</span>
              <span v-if="team.venue.capacity">👥 {{ team.venue.capacity?.toLocaleString('tr-TR') }} kapasite</span>
              <span v-if="team.venue.surface">🌱 {{ team.venue.surface }}</span>
            </div>
          </div>
        </div>
      </section>

      <!-- KADRO -->
      <section v-if="team.squad?.length" class="profile-section">
        <h2 class="section-title">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="section-icon">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
            <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
            <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
          </svg>
          Kadro
        </h2>

        <div v-for="(players, position) in squadByPosition" :key="position" class="squad-group">
          <h3 class="squad-position-title">{{ positionLabel(position) }}</h3>
          <div class="squad-grid">
            <div
              v-for="player in players"
              :key="player.id"
              class="squad-card"
              @click="goToPlayer(player.id)"
            >
              <img
                v-if="player.photo"
                :src="player.photo"
                :alt="player.name"
                class="squad-photo"
                @error="$event.target.style.display='none'"
              />
              <div v-else class="squad-photo-placeholder">{{ player.name?.charAt(0) }}</div>
              <div class="squad-details">
                <span class="squad-number" v-if="player.number">#{{ player.number }}</span>
                <span class="squad-name">{{ player.name }}</span>
                <span class="squad-age" v-if="player.age">{{ player.age }} yaş</span>
              </div>
            </div>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { fetchTeamProfile } from '../api/teamApi'
import { useAuthStore } from '../stores/auth'
import LoadingSpinner from '../components/common/LoadingSpinner.vue'

const route     = useRoute()
const router    = useRouter()
const authStore = useAuthStore()

const team      = ref(null)
const isLoading = ref(false)
const error     = ref('')

const POSITION_ORDER = ['Goalkeeper', 'Defender', 'Midfielder', 'Attacker']

const positionLabel = (pos) => ({
  Goalkeeper: '🧤 Kaleciler',
  Defender:   '🛡️ Defanslar',
  Midfielder: '⚙️ Orta Sahalar',
  Attacker:   '⚡ Forvetler'
})[pos] ?? pos

const squadByPosition = computed(() => {
  if (!team.value?.squad) return {}
  const groups = {}
  for (const p of team.value.squad) {
    const pos = p.position ?? 'Other'
    if (!groups[pos]) groups[pos] = []
    groups[pos].push(p)
  }
  const sorted = {}
  for (const pos of POSITION_ORDER) {
    if (groups[pos]?.length) sorted[pos] = groups[pos].sort((a, b) => (a.number ?? 99) - (b.number ?? 99))
  }
  for (const pos of Object.keys(groups)) {
    if (!sorted[pos]) sorted[pos] = groups[pos]
  }
  return sorted
})

const goToPlayer = (playerId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/player/${playerId}`)
}

onMounted(async () => {
  const id = parseInt(route.params.id)
  if (!id) { error.value = 'Geçersiz takım ID.'; return }
  isLoading.value = true
  try {
    team.value = await fetchTeamProfile(id)
  } catch (e) {
    error.value = e.response?.data?.message || 'Kulüp bilgileri yüklenemedi.'
  } finally {
    isLoading.value = false
  }
})
</script>

<style scoped>
.profile-page {
  max-width: 900px;
  margin: 0 auto;
  padding: 1.5rem 1rem;
}

/* ── Error ── */
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  padding: 4rem 2rem;
  color: #8b949e;
  text-align: center;
}
.error-icon { font-size: 2.5rem; }

/* ── Hero ── */
.team-hero {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 1.5rem;
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 16px;
  margin-bottom: 1.5rem;
}
.team-hero-logo {
  width: 90px;
  height: 90px;
  object-fit: contain;
  flex-shrink: 0;
}
.team-hero-name {
  font-size: 1.7rem;
  font-weight: 800;
  color: #e6edf3;
  margin: 0 0 0.6rem;
}
.team-hero-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.meta-chip {
  font-size: 0.78rem;
  background: #21262d;
  color: #8b949e;
  padding: 0.25rem 0.65rem;
  border-radius: 20px;
  border: 1px solid #30363d;
}
.meta-chip--special { background: #1f2d1a; color: #56d364; border-color: #238636; }

/* ── Sections ── */
.profile-section {
  margin-bottom: 2rem;
}
.section-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 700;
  color: #e6edf3;
  margin: 0 0 1rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #21262d;
}
.section-icon {
  width: 18px;
  height: 18px;
  color: #58a6ff;
  flex-shrink: 0;
}

/* ── Venue ── */
.venue-card {
  display: flex;
  gap: 1.25rem;
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 12px;
  overflow: hidden;
}
.venue-image {
  width: 220px;
  height: 130px;
  object-fit: cover;
  flex-shrink: 0;
}
.venue-info {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  justify-content: center;
}
.venue-name {
  font-size: 1rem;
  font-weight: 700;
  color: #e6edf3;
  margin: 0;
}
.venue-details {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  font-size: 0.82rem;
  color: #8b949e;
}

/* ── Squad ── */
.squad-group { margin-bottom: 1.5rem; }
.squad-position-title {
  font-size: 0.78rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  color: #484f58;
  margin: 0 0 0.75rem;
}
.squad-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 0.75rem;
}
.squad-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 0.75rem;
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.15s;
  text-align: center;
}
.squad-card:hover {
  border-color: #58a6ff40;
  background: #1c2129;
  transform: translateY(-2px);
}
.squad-photo {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #21262d;
}
.squad-photo-placeholder {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: #21262d;
  border: 2px solid #30363d;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
  font-weight: 700;
  color: #58a6ff;
}
.squad-details {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.15rem;
}
.squad-number { font-size: 0.7rem; color: #58a6ff; font-weight: 700; }
.squad-name   { font-size: 0.82rem; color: #e6edf3; font-weight: 600; line-height: 1.3; }
.squad-age    { font-size: 0.72rem; color: #8b949e; }

@media (max-width: 600px) {
  .team-hero       { flex-direction: column; text-align: center; }
  .team-hero-meta  { justify-content: center; }
  .venue-card      { flex-direction: column; }
  .venue-image     { width: 100%; height: 160px; }
  .squad-grid      { grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); }
}
</style>
