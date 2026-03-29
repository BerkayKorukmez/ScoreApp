<template>
  <div class="league-group">
    <!-- Lig başlığı -->
    <div class="league-header">
      <div class="league-title">
        <img
          v-if="flag"
          :src="flag"
          class="league-flag"
          @error="$event.target.style.display='none'"
        />
        <span v-else class="league-icon">🏆</span>
        <span v-if="country" class="league-country-label">{{ country }} ·</span>
        <span class="league-name">{{ leagueName }}</span>
      </div>
    </div>

    <!-- O ligteki maçlar -->
    <MatchRow
      v-for="match in matches"
      :key="match.id"
      :match="match"
      :league-name="leagueName"
      :sport="sport"
      :is-favorite="isFavorite(match.id)"
      :is-authenticated="isAuthenticated"
      @click="$emit('match-click', $event)"
      @toggle-favorite="$emit('toggle-favorite', $event)"
    />
  </div>
</template>

<script setup>
import MatchRow from './MatchRow.vue'

defineProps({
  leagueKey:       { type: String,   required: true },
  leagueName:      { type: String,   required: true },
  /** Ana sayfa spor seçimi — AI önizlemesi için */
  sport:           { type: String,   default: 'football' },
  country:         { type: String,   default: null },
  flag:            { type: String,   default: null },
  matches:         { type: Array,    required: true },
  isFavorite:      { type: Function, default: () => false },
  isAuthenticated: { type: Boolean,  default: false }
})

defineEmits(['match-click', 'toggle-favorite'])
</script>

<style scoped>
.league-group { border-bottom: 1px solid #21262d; }

.league-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.6rem 1.25rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.league-title  { display: flex; align-items: center; gap: 0.4rem; }
.league-icon   { font-size: 0.85rem; }

.league-flag {
  width: 18px;
  height: 13px;
  object-fit: cover;
  border-radius: 2px;
  flex-shrink: 0;
}

.league-country-label {
  font-size: 0.72rem;
  font-weight: 500;
  color: #8b949e;
  text-transform: uppercase;
  letter-spacing: 0.2px;
}

.league-name {
  font-size: 0.8rem;
  font-weight: 600;
  color: #c9d1d9;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}
</style>
