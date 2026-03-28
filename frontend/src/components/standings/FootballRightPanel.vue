<template>
  <aside class="football-right-panel">
    <!-- Sekmeler - en üstte, her zaman görünür -->
    <div class="fp-tabs">
      <button
        type="button"
        :class="['fp-tab', { active: activeTab === 'standings' }]"
        @click="activeTab = 'standings'"
      >
        📊 Puan Tablosu
      </button>
      <button
        type="button"
        :class="['fp-tab', { active: activeTab === 'goals' }]"
        @click="activeTab = 'goals'"
      >
        ⚽ Gol Krallığı
      </button>
    </div>

    <div class="fp-content">
      <StandingsPanel
        v-show="activeTab === 'standings'"
        :league-info="leagueInfo"
        :standings="standings"
        :is-loading="isStandingsLoading"
        sport="football"
        @select-league="$emit('select-league', $event)"
      />
      <GoalKingsPanel v-show="activeTab === 'goals'" />
    </div>
  </aside>
</template>

<script setup>
import { ref } from 'vue'
import StandingsPanel from './StandingsPanel.vue'
import GoalKingsPanel from './GoalKingsPanel.vue'

defineProps({
  leagueInfo: { type: Object, default: null },
  standings: { type: Array, default: () => [] },
  isStandingsLoading: { type: Boolean, default: false }
})

defineEmits(['select-league'])

const activeTab = ref('standings')
</script>

<style scoped>
.football-right-panel {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  background: #0d1117;
  border-left: 1px solid #21262d;
  max-height: calc(100vh - 112px);
}

.fp-tabs {
  display: flex;
  flex-shrink: 0;
  background: #161b22;
  border-bottom: 2px solid #21262d;
  padding: 0.5rem;
  gap: 0.5rem;
}

.fp-tab {
  flex: 1;
  padding: 0.6rem 0.75rem;
  background: #21262d;
  color: #8b949e;
  border: 1px solid #30363d;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-family: inherit;
}

.fp-tab:hover {
  background: #30363d;
  color: #c9d1d9;
}

.fp-tab.active {
  background: #58a6ff;
  color: #fff;
  border-color: #58a6ff;
}

.fp-content {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.fp-content :deep(.right-panel) {
  flex: 1;
  min-height: 0;
  max-height: none;
  position: static;
  overflow-y: auto;
}
</style>
