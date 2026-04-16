<template>
  <aside class="right-panel goal-kings-panel">
    <div class="gk-header">
      <h3 class="gk-title">⚽ Gol Krallığı</h3>
      <p class="gk-sub">Lig seçerek gol krallarını görüntüleyin</p>
    </div>

    <div class="gk-league-select">
      <select v-model="selectedLeagueId" class="gk-select" @change="loadGoalKings">
        <option :value="null">Lig seçin</option>
        <template v-for="group in leaguesByCountry" :key="group.country">
          <option disabled :value="null">— {{ group.country }} —</option>
          <option
            v-for="league in group.leagues"
            :key="league.key"
            :value="league.leagueId"
          >
            {{ league.displayName || league.name }}
          </option>
        </template>
      </select>
    </div>

    <LoadingSpinner v-if="isLoading" :small="true" label="Gol kralları yükleniyor..." />

    <div v-else-if="goalKings.length === 0" class="gk-empty">
      <span class="gk-empty-icon">🏆</span>
      <p>Lig seçin veya bu lig için veri bulunamadı</p>
    </div>

    <div v-else class="gk-list">
      <div
        v-for="(player, index) in goalKings"
        :key="player.playerId || player.name"
        class="gk-row"
        :class="{ 'gk-row--top3': index < 3, 'gk-row--clickable': !!player.playerId }"
        @click="player.playerId && goToPlayer(player.playerId)"
      >
        <span class="gk-rank">{{ index + 1 }}</span>
        <div class="gk-player">
          <img
            v-if="player.photo"
            :src="player.photo"
            :alt="player.name"
            class="gk-photo"
            @error="$event.target.style.display='none'"
          />
          <div v-else class="gk-photo-placeholder">{{ player.name?.charAt(0) }}</div>
          <div class="gk-player-info">
            <span class="gk-name">{{ player.name }}</span>
            <span v-if="player.team" class="gk-team">
              <img v-if="player.teamLogo" :src="player.teamLogo" class="gk-team-logo" @error="$event.target.style.display='none'" />
              {{ player.team }}
            </span>
          </div>
        </div>
        <span class="gk-goals">⚽ {{ player.goals }}</span>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { fetchGoalKings } from '../../api/matchApi'
import { POPULAR_STANDINGS_LEAGUES } from '../../constants/sports'
import LoadingSpinner from '../common/LoadingSpinner.vue'

const router    = useRouter()
const authStore = useAuthStore()

const goToPlayer = (playerId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/player/${playerId}`)
}

// Süper Lig varsayılan (leagueId: 203)
const selectedLeagueId = ref(203)
const goalKings = ref([])
const isLoading = ref(false)

const leaguesByCountry = computed(() => {
  const list = POPULAR_STANDINGS_LEAGUES.football ?? []
  const map = new Map()
  const byCountry = {}

  list
    .filter((l) => l.leagueId)
    .forEach((l) => {
      const key = `${l.country}::${l.name}`
      if (!map.has(key)) {
        map.set(key, l)
        if (!byCountry[l.country]) byCountry[l.country] = []
        byCountry[l.country].push(l)
      }
    })

  const order = ['Turkey', 'England', 'Spain', 'Italy', 'Germany', 'France', 'World']
  const sorted = Object.keys(byCountry).sort((a, b) => {
    const ia = order.indexOf(a)
    const ib = order.indexOf(b)
    if (ia !== -1 && ib !== -1) return ia - ib
    if (ia !== -1) return -1
    if (ib !== -1) return 1
    return a.localeCompare(b)
  })

  return sorted.map((country) => ({ country, leagues: byCountry[country] }))
})

const loadGoalKings = async () => {
  const id = selectedLeagueId.value
  if (!id) {
    goalKings.value = []
    return
  }

  isLoading.value = true
  goalKings.value = []
  try {
    goalKings.value = await fetchGoalKings(id)
  } catch (err) {
    console.error('Gol kralları yüklenemedi:', err)
  } finally {
    isLoading.value = false
  }
}

watch(selectedLeagueId, (v) => {
  if (v) loadGoalKings()
}, { immediate: true })
</script>

<style scoped>
.goal-kings-panel {
  background: #0d1117;
  overflow-y: auto;
  max-height: calc(100vh - 112px);
  position: sticky;
  top: 112px;
}

.gk-header {
  padding: 1rem 1.25rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.gk-title {
  font-size: 1rem;
  font-weight: 700;
  color: #e6edf3;
  margin: 0 0 0.25rem 0;
}

.gk-sub {
  font-size: 0.72rem;
  color: #8b949e;
  margin: 0;
}

.gk-league-select {
  padding: 0.75rem 1.25rem;
  border-bottom: 1px solid #21262d;
}

.gk-select {
  width: 100%;
  background: #161b22;
  color: #c9d1d9;
  border: 1px solid #30363d;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  font-size: 0.85rem;
  cursor: pointer;
  outline: none;
}

.gk-select:focus {
  border-color: #58a6ff;
}

.gk-list {
  padding: 0.5rem 0;
}

.gk-row {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.55rem 1rem;
  border-bottom: 1px solid #21262d40;
  font-size: 0.85rem;
  transition: background 0.13s;
}
.gk-row:hover { background: #161b22; }
.gk-row--clickable { cursor: pointer; }
.gk-row--clickable:hover .gk-name { color: #58a6ff; }
.gk-row--top3 { font-weight: 600; color: #e6edf3; }

.gk-rank {
  width: 1.4rem;
  flex-shrink: 0;
  font-weight: 700;
  color: #8b949e;
  text-align: right;
  font-size: 0.78rem;
}

.gk-player {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  flex: 1;
  min-width: 0;
}
.gk-photo {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
  border: 1px solid #30363d;
}
.gk-photo-placeholder {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: #21262d;
  border: 1px solid #30363d;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: 700;
  color: #58a6ff;
  flex-shrink: 0;
}
.gk-player-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.gk-name {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 0.83rem;
  color: #c9d1d9;
  transition: color 0.13s;
}
.gk-team {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.7rem;
  color: #484f58;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.gk-team-logo {
  width: 12px;
  height: 12px;
  object-fit: contain;
}
.gk-goals {
  font-weight: 700;
  color: #56d364;
  font-size: 0.82rem;
  white-space: nowrap;
  flex-shrink: 0;
}

.gk-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem 1.25rem;
  color: #8b949e;
  text-align: center;
}

.gk-empty-icon {
  font-size: 2rem;
  opacity: 0.5;
}

.gk-empty p {
  margin: 0;
  font-size: 0.85rem;
}
</style>
