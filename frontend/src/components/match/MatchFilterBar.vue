<template>
  <div class="filter-bar">
    <!-- Filtre sekmeleri -->
    <div class="filter-tabs">
      <button
        :class="['tab-btn', { active: activeFilter === 'live' }]"
        @click="$emit('update:activeFilter', 'live')"
      >
        <span class="tab-dot live-dot"></span>
        Canlı
        <span v-if="liveMatchCount > 0" class="tab-count">{{ liveMatchCount }}</span>
      </button>
      <button
        :class="['tab-btn', { active: activeFilter === 'all' }]"
        @click="$emit('update:activeFilter', 'all')"
      >
        Tümü
      </button>
      <button
        :class="['tab-btn', { active: activeFilter === 'finished' }]"
        @click="$emit('update:activeFilter', 'finished')"
      >
        Tamamlanan
      </button>
      <button
        v-if="isAuthenticated"
        :class="['tab-btn', { active: activeFilter === 'favorites' }]"
        @click="$emit('update:activeFilter', 'favorites')"
      >
        ⭐ Favoriler
      </button>
    </div>

    <!-- Lig seçim dropdown'u -->
    <div class="league-selector">
      <select
        :value="selectedLeagueKey"
        class="league-dropdown"
        @change="$emit('update:selectedLeagueKey', $event.target.value === '' ? null : $event.target.value)"
      >
        <option value="">Tüm Ligler</option>
        <optgroup
          v-for="group in leaguesByCountry"
          :key="group.country"
          :label="group.country"
        >
          <option
            v-for="league in group.leagues"
            :key="league.key"
            :value="league.key"
          >
            {{ league.name }}
          </option>
        </optgroup>
      </select>
    </div>
  </div>
</template>

<script setup>
defineProps({
  activeFilter:      { type: String,  required: true },
  selectedLeagueKey: { type: String,  default: null },
  leaguesByCountry:  { type: Array,   default: () => [] },
  liveMatchCount:    { type: Number,  default: 0 },
  isAuthenticated:   { type: Boolean, default: false }
})

defineEmits(['update:activeFilter', 'update:selectedLeagueKey'])
</script>

<style scoped>
.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1.25rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
  position: sticky;
  top: 0;
  z-index: 10;
}

.filter-tabs { display: flex; gap: 0.25rem; }

.tab-btn {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.85rem;
  background: transparent;
  border: 1px solid transparent;
  color: #8b949e;
  font-size: 0.8rem;
  font-weight: 500;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.tab-btn:hover  { color: #c9d1d9; background: #21262d; }
.tab-btn.active { color: #ffffff; background: #21262d; border-color: #30363d; }

.tab-dot { width: 6px; height: 6px; border-radius: 50%; }

.live-dot {
  background: #f85149;
  box-shadow: 0 0 6px #f8514966;
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50%       { opacity: 0.4; }
}

.tab-count {
  background: #f8514933;
  color: #f85149;
  padding: 0.05rem 0.4rem;
  border-radius: 8px;
  font-size: 0.7rem;
  font-weight: 700;
}

.league-selector { flex-shrink: 0; }

.league-dropdown {
  background: #0d1117;
  color: #c9d1d9;
  border: 1px solid #30363d;
  padding: 0.4rem 0.75rem;
  border-radius: 6px;
  font-size: 0.8rem;
  cursor: pointer;
  min-width: 160px;
  outline: none;
  transition: border-color 0.2s;
}

.league-dropdown:focus   { border-color: #58a6ff; }
.league-dropdown option  { background: #161b22; }
.league-dropdown optgroup { background: #0d1117; color: #58a6ff; font-weight: 600; font-size: 0.75rem; }

@media (max-width: 900px) {
  .filter-bar { flex-direction: column; gap: 0.5rem; align-items: flex-start; }
  .league-dropdown { width: 100%; }
}
</style>
