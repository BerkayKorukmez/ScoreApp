<template>
  <div class="app-layout">
    <AppHeader />
    <main>
      <router-view />
    </main>
  </div>
</template>

<script setup>
import AppHeader from '../components/AppHeader.vue'
</script>

<style scoped>
.app-layout {
  min-height: 100vh;
  background: #0e1118;
  color: #e1e4e8;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}
</style>
