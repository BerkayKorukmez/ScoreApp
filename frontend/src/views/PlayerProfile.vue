<template>
  <div class="profile-page">
    <LoadingSpinner v-if="isLoading" label="Oyuncu yükleniyor..." />

    <div v-else-if="error" class="error-state">
      <span class="error-icon">⚠️</span>
      <p>{{ error }}</p>
    </div>

    <template v-else-if="player">
      <!-- HEADER -->
      <div class="player-hero">
        <img
          v-if="player.photo"
          :src="player.photo"
          :alt="player.name"
          class="player-hero-photo"
          @error="$event.target.style.display='none'"
        />
        <div v-else class="player-hero-placeholder">{{ player.name?.charAt(0) }}</div>

        <div class="player-hero-info">
          <div class="player-hero-top">
            <h1 class="player-hero-name">{{ player.name }}</h1>
            <span v-if="player.injured" class="injured-badge">🩹 Sakatlanmış</span>
          </div>
          <div class="player-hero-meta">
            <span v-if="player.nationality" class="meta-chip">🌍 {{ player.nationality }}</span>
            <span v-if="player.age" class="meta-chip">🎂 {{ player.age }} yaş</span>
            <span v-if="player.birthDate" class="meta-chip">{{ formatDate(player.birthDate) }}</span>
            <span v-if="player.birthPlace" class="meta-chip">📍 {{ player.birthPlace }}<template v-if="player.birthCountry">, {{ player.birthCountry }}</template></span>
            <span v-if="player.height" class="meta-chip">📏 {{ player.height }}</span>
            <span v-if="player.weight" class="meta-chip">⚖️ {{ player.weight }}</span>
          </div>
        </div>
      </div>

      <!-- İSTATİSTİKLER -->
      <section v-if="player.stats?.length" class="profile-section">
        <h2 class="section-title">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="section-icon">
            <line x1="18" y1="20" x2="18" y2="10"/>
            <line x1="12" y1="20" x2="12" y2="4"/>
            <line x1="6"  y1="20" x2="6"  y2="14"/>
          </svg>
          Sezon İstatistikleri
        </h2>

        <div class="stats-list">
          <div v-for="(stat, i) in player.stats" :key="i" class="stat-card">
            <div class="stat-card-header">
              <div class="stat-league">
                <img
                  v-if="stat.leagueLogo"
                  :src="stat.leagueLogo"
                  class="stat-league-logo"
                  @error="$event.target.style.display='none'"
                />
                <div>
                  <span class="stat-league-name">{{ stat.leagueName }}</span>
                  <span class="stat-team-name">
                    <img
                      v-if="stat.teamLogo"
                      :src="stat.teamLogo"
                      class="stat-team-logo"
                      @error="$event.target.style.display='none'"
                    />
                    {{ stat.teamName }}
                  </span>
                </div>
              </div>
              <span v-if="stat.position" class="stat-position">{{ positionLabel(stat.position) }}</span>
            </div>

            <div class="stat-grid">
              <div class="stat-item">
                <span class="stat-val">{{ stat.appearances ?? 0 }}</span>
                <span class="stat-lbl">Maç</span>
              </div>
              <div class="stat-item">
                <span class="stat-val">{{ stat.minutes ?? 0 }}</span>
                <span class="stat-lbl">Dakika</span>
              </div>
              <div class="stat-item">
                <span class="stat-val stat-val--goals">{{ stat.goals ?? 0 }}</span>
                <span class="stat-lbl">Gol</span>
              </div>
              <div class="stat-item">
                <span class="stat-val stat-val--assists">{{ stat.assists ?? 0 }}</span>
                <span class="stat-lbl">Asist</span>
              </div>
              <div class="stat-item">
                <span class="stat-val stat-val--yellow">{{ stat.yellowCards ?? 0 }}</span>
                <span class="stat-lbl">Sarı</span>
              </div>
              <div class="stat-item">
                <span class="stat-val stat-val--red">{{ stat.redCards ?? 0 }}</span>
                <span class="stat-lbl">Kırmızı</span>
              </div>
              <div v-if="stat.shotsTotal != null" class="stat-item">
                <span class="stat-val">{{ stat.shotsTotal }}</span>
                <span class="stat-lbl">Şut</span>
              </div>
              <div v-if="stat.rating" class="stat-item">
                <span class="stat-val stat-val--rating">{{ parseFloat(stat.rating).toFixed(1) }}</span>
                <span class="stat-lbl">Rating</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <div v-else class="empty-stats">
        <p>Bu sezon için istatistik bulunamadı.</p>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { fetchPlayerProfile } from '../api/teamApi'
import LoadingSpinner from '../components/common/LoadingSpinner.vue'

const route     = useRoute()
const player    = ref(null)
const isLoading = ref(false)
const error     = ref('')

const positionLabel = (pos) => ({
  Goalkeeper: '🧤 Kaleci',
  Defender:   '🛡️ Defans',
  Midfielder: '⚙️ Orta Saha',
  Forward:    '⚡ Forvet',
  Attacker:   '⚡ Forvet',
})[pos] ?? pos

const formatDate = (dateStr) => {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  if (isNaN(d)) return dateStr
  return d.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })
}

onMounted(async () => {
  const id = parseInt(route.params.id)
  if (!id) { error.value = 'Geçersiz oyuncu ID.'; return }
  isLoading.value = true
  try {
    player.value = await fetchPlayerProfile(id)
  } catch (e) {
    error.value = e.response?.data?.message || 'Oyuncu bilgileri yüklenemedi.'
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

/* ── Error / Empty ── */
.error-state, .empty-stats {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  padding: 4rem 2rem;
  color: #8b949e;
  text-align: center;
  font-size: 0.9rem;
}
.error-icon { font-size: 2.5rem; }

/* ── Hero ── */
.player-hero {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 1.5rem;
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 16px;
  margin-bottom: 1.5rem;
}
.player-hero-photo {
  width: 90px;
  height: 90px;
  border-radius: 50%;
  object-fit: cover;
  border: 3px solid #21262d;
  flex-shrink: 0;
}
.player-hero-placeholder {
  width: 90px;
  height: 90px;
  border-radius: 50%;
  background: #21262d;
  border: 3px solid #30363d;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2rem;
  font-weight: 700;
  color: #58a6ff;
  flex-shrink: 0;
}
.player-hero-top {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
  margin-bottom: 0.6rem;
}
.player-hero-name {
  font-size: 1.7rem;
  font-weight: 800;
  color: #e6edf3;
  margin: 0;
}
.injured-badge {
  font-size: 0.75rem;
  background: #3f1f1f;
  color: #fc8181;
  border: 1px solid #fc818140;
  padding: 0.2rem 0.6rem;
  border-radius: 20px;
}
.player-hero-meta {
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

/* ── Section ── */
.profile-section { margin-bottom: 2rem; }
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
.section-icon { width: 18px; height: 18px; color: #58a6ff; flex-shrink: 0; }

/* ── Stats ── */
.stats-list { display: flex; flex-direction: column; gap: 0.75rem; }

.stat-card {
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 12px;
  overflow: hidden;
}
.stat-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid #21262d;
  gap: 0.5rem;
}
.stat-league {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  min-width: 0;
}
.stat-league-logo {
  width: 22px;
  height: 22px;
  object-fit: contain;
  flex-shrink: 0;
}
.stat-league-name {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  color: #e6edf3;
}
.stat-team-name {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.75rem;
  color: #8b949e;
  margin-top: 0.15rem;
}
.stat-team-logo {
  width: 14px;
  height: 14px;
  object-fit: contain;
}
.stat-position {
  font-size: 0.72rem;
  background: #21262d;
  color: #8b949e;
  padding: 0.2rem 0.55rem;
  border-radius: 20px;
  white-space: nowrap;
  flex-shrink: 0;
}

.stat-grid {
  display: flex;
  flex-wrap: wrap;
  padding: 0.75rem 0.5rem;
}
.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.15rem;
  min-width: 70px;
  flex: 1;
  padding: 0.5rem;
}
.stat-val {
  font-size: 1.3rem;
  font-weight: 800;
  color: #e6edf3;
  line-height: 1;
}
.stat-val--goals   { color: #56d364; }
.stat-val--assists { color: #58a6ff; }
.stat-val--yellow  { color: #e3b341; }
.stat-val--red     { color: #f85149; }
.stat-val--rating  { color: #a78bfa; }
.stat-lbl {
  font-size: 0.68rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #484f58;
}

@media (max-width: 600px) {
  .player-hero  { flex-direction: column; text-align: center; }
  .player-hero-top { justify-content: center; }
  .player-hero-meta { justify-content: center; }
  .stat-item    { min-width: 60px; }
}
</style>
