<template>
  <div class="home-page">

    <SportNavBar
      :sports="SPORTS"
      :selected-sport="selectedSport"
      :match-counts="sportMatchCounts"
      @update:selected-sport="onSportChange"
    />

    <div class="main-layout">
      <!-- SOL PANEL -->
      <div class="left-panel">
        <MatchFilterBar
          :active-filter="activeFilter"
          :selected-league-key="selectedLeagueKey"
          :leagues-by-country="leaguesByCountry"
          :live-match-count="liveMatchCount"
          :is-authenticated="authStore.isAuthenticated"
          @update:active-filter="activeFilter = $event"
          @update:selected-league-key="onLeagueKeyChange"
        />

        <LoadingSpinner v-if="isLoading" label="Maçlar yükleniyor..." />

        <EmptyState v-else-if="Object.keys(groupedMatches).length === 0" />

        <div v-else class="matches-container">
          <LeagueGroup
            v-for="(group, leagueKey) in groupedMatches"
            :key="leagueKey"
            :league-key="leagueKey"
            :league-name="getLeagueName(leagueKey)"
            :country="group[0]?.leagueCountry || null"
            :flag="group[0]?.leagueFlag || null"
            :matches="group"
            :is-favorite="(id) => favoriteMatchIds.includes(id)"
            :is-authenticated="authStore.isAuthenticated"
            @match-click="goToMatchDetail"
            @toggle-favorite="toggleFavorite"
          />
        </div>
      </div>

      <!-- SAĞ PANEL -->
      <StandingsPanel
        :league-info="activeStandingsLeague"
        :standings="leagueStandings"
        :is-loading="isStandingsLoading"
        :sport="selectedSport"
        @select-league="onStandingsLeagueSelect"
      />
    </div>

  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useMatches }   from '../composables/useMatches'
import { useStandings } from '../composables/useStandings'
import { useFavorites } from '../composables/useFavorites'
import { useLeagues }   from '../composables/useLeagues'
import { SPORTS }       from '../constants/sports'

import SportNavBar     from '../components/match/SportNavBar.vue'
import MatchFilterBar  from '../components/match/MatchFilterBar.vue'
import LeagueGroup     from '../components/match/LeagueGroup.vue'
import LoadingSpinner  from '../components/common/LoadingSpinner.vue'
import EmptyState      from '../components/common/EmptyState.vue'
import StandingsPanel  from '../components/standings/StandingsPanel.vue'

const router    = useRouter()
const authStore = useAuthStore()

const selectedSport           = ref('football')
const activeFilter            = ref('all')
const standingsLeagueOverride = ref(null) // Popüler lig butonundan seçilenler

/* ---------- Composables ---------- */
const { favoriteMatchIds, toggleFavorite } = useFavorites()

// 1) Maç çekme + WebSocket
const { matches, isLoading } = useMatches(selectedSport)

// 2) Lig filtreleme, gruplama, sayaçlar — selectedLeagueKey burada yönetiliyor
const {
  selectedLeagueKey,
  leaguesByCountry,
  selectedLeagueInfo,
  groupedMatches,
  liveMatchCount,
  sportMatchCounts
} = useLeagues(matches, selectedSport, activeFilter, favoriteMatchIds)

// 3) Puan tablosu — maçtan seçilen lig VEYA popüler lig butonundan gelen
const activeStandingsLeague = computed(() =>
  standingsLeagueOverride.value || selectedLeagueInfo.value
)

const {
  leagueStandings,
  isStandingsLoading
} = useStandings(selectedSport, activeStandingsLeague)

/* ---------- Yardımcılar ---------- */
const getLeagueName = (leagueKey) => {
  const parts = leagueKey.split('::')
  return parts.length > 1 ? parts[1] : leagueKey
}

const goToMatchDetail = (matchId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/match/${matchId}`)
}

/* ---------- Olay işleyiciler ---------- */
const onSportChange = (sport) => {
  selectedSport.value = sport
  activeFilter.value  = 'all'
  standingsLeagueOverride.value = null
}

const onLeagueKeyChange = (key) => {
  selectedLeagueKey.value       = key
  standingsLeagueOverride.value = null // Match filtresi değişince override'ı temizle
}

// StandingsPanel'den: popüler lig seçildi (null = geri/temizle)
const onStandingsLeagueSelect = (league) => {
  if (!league) {
    standingsLeagueOverride.value = null
    return
  }
  standingsLeagueOverride.value = league
  selectedLeagueKey.value       = null // Sol paneldeki lig filtresini temizle
}

// Maçlardan seçilen lig değişince override'ı kaldır
watch(selectedLeagueInfo, (val) => {
  if (val) standingsLeagueOverride.value = null
})
</script>

<style scoped>
.main-layout {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 0;
  max-width: 1400px;
  margin: 0 auto;
  min-height: calc(100vh - 112px);
}

.left-panel {
  border-right: 1px solid #21262d;
  overflow-y: auto;
}

@media (max-width: 900px) {
  .main-layout { grid-template-columns: 1fr; }
}
</style>
