<template>
  <aside class="right-panel goal-kings-panel">
    <div class="gk-header">
      <h3 class="gk-title">⚽ Gol Krallığı</h3>
      <p class="gk-sub">Lig seçerek gol krallarını görüntüleyin</p>
    </div>

    <div class="gk-league-select">
      <select v-model="selectedLeagueKey" class="gk-select" @change="loadGoalKings">
        <option value="">Lig seçin</option>
        <template v-for="group in leaguesByCountry" :key="group.country">
          <option disabled value="">— {{ group.country }} —</option>
          <option
            v-for="league in group.leagues"
            :key="league.key"
            :value="league.collectApiKey"
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
        :key="player.name"
        class="gk-row"
        :class="{ 'gk-row--top3': index < 3 }"
      >
        <span class="gk-rank">{{ index + 1 }}</span>
        <span class="gk-name">{{ player.name }}</span>
        <span class="gk-goals">{{ player.goals }}</span>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { fetchGoalKings } from '../../api/matchApi'
import { POPULAR_STANDINGS_LEAGUES } from '../../constants/sports'
import LoadingSpinner from '../common/LoadingSpinner.vue'

const selectedLeagueKey = ref('super-lig')
const goalKings = ref([])
const isLoading = ref(false)

const leaguesByCountry = computed(() => {
  const list = POPULAR_STANDINGS_LEAGUES.football ?? []
  const map = new Map()
  const byCountry = {}

  list
    .filter((l) => l.collectApiKey)
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
  const key = selectedLeagueKey.value
  if (!key) {
    goalKings.value = []
    return
  }

  isLoading.value = true
  goalKings.value = []
  try {
    goalKings.value = await fetchGoalKings(key)
  } catch (err) {
    console.error('Gol kralları yüklenemedi:', err)
  } finally {
    isLoading.value = false
  }
}

watch(selectedLeagueKey, (v) => {
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
  gap: 0.75rem;
  padding: 0.5rem 1.25rem;
  border-bottom: 1px solid #21262d40;
  font-size: 0.85rem;
}

.gk-row:hover {
  background: #161b22;
}

.gk-row--top3 {
  font-weight: 600;
  color: #e6edf3;
}

.gk-rank {
  width: 1.5rem;
  flex-shrink: 0;
  font-weight: 700;
  color: #8b949e;
  text-align: right;
}

.gk-name {
  flex: 1;
  min-width: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.gk-goals {
  font-weight: 700;
  color: #58a6ff;
  min-width: 1.5rem;
  text-align: right;
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
