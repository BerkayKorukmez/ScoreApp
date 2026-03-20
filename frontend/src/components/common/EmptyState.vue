<template>
  <div class="empty-state">
    <span class="empty-icon">{{ icon }}</span>
    <p>{{ title }}</p>
    <small>{{ subtitle }}</small>
  </div>
</template>

<script setup>
defineProps({
  icon:     { type: String, default: '📭' },
  title:    { type: String, default: 'Maç bulunamadı' },
  subtitle: { type: String, default: 'Farklı bir filtre veya lig seçmeyi deneyin' }
})
</script>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 3rem;
  color: #8b949e;
  text-align: center;
}

.empty-icon { font-size: 2.5rem; opacity: 0.5; }
.empty-state p     { font-size: 1rem; font-weight: 500; margin: 0; }
.empty-state small { font-size: 0.8rem; color: #484f58; }
</style>
