<template>
  <nav class="sport-nav">
    <button
      v-for="sport in sports"
      :key="sport.id"
      :class="['sport-tab', { active: selectedSport === sport.id }]"
      @click="$emit('update:selectedSport', sport.id)"
    >
      <span class="sport-tab-icon">{{ sport.icon }}</span>
      <span class="sport-tab-label">{{ sport.label }}</span>
      <span v-if="matchCounts[sport.id]" class="sport-tab-count">
        {{ matchCounts[sport.id] }}
      </span>
    </button>
  </nav>
</template>

<script setup>
defineProps({
  sports:        { type: Array,  required: true },
  selectedSport: { type: String, required: true },
  matchCounts:   { type: Object, default: () => ({}) }
})

defineEmits(['update:selectedSport'])
</script>

<style scoped>
.sport-nav {
  display: flex;
  align-items: center;
  background: #161b22;
  border-bottom: 1px solid #21262d;
  padding: 0 1.5rem;
  height: 56px;
  overflow-x: auto;
  flex-shrink: 0;
}

.sport-tab {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.75rem 1.25rem;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: #8b949e;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.sport-tab:hover  { color: #c9d1d9; background: #1c2129; }
.sport-tab.active { color: #58a6ff; border-bottom-color: #58a6ff; }

.sport-tab-icon  { font-size: 1.05rem; }

.sport-tab-count {
  background: #30363d;
  color: #8b949e;
  padding: 0.1rem 0.45rem;
  border-radius: 10px;
  font-size: 0.7rem;
  font-weight: 600;
}

.sport-tab.active .sport-tab-count {
  background: #58a6ff33;
  color: #58a6ff;
}

@media (max-width: 600px) {
  .sport-nav { padding: 0 0.5rem; }
  .sport-tab { padding: 0.6rem 0.75rem; font-size: 0.78rem; }
  .sport-tab-label { display: none; }
}
</style>
