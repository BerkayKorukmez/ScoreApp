<template>
  <Teleport to="body">
    <div v-if="open" class="mp-overlay" @click.self="$emit('close')">
      <div class="mp-modal" role="dialog" aria-labelledby="mp-title">
        <div class="mp-head">
          <h2 id="mp-title">Yapay zeka yorumu</h2>
          <button type="button" class="mp-close" aria-label="Kapat" @click="$emit('close')">×</button>
        </div>
        <p class="mp-sub">{{ homeTeam }} <span class="mp-vs">vs</span> {{ awayTeam }}</p>

        <div v-if="loading" class="mp-body mp-loading">
          <div class="mp-spinner" />
          <span>Analiz hazırlanıyor…</span>
        </div>

        <div v-else-if="error" class="mp-body mp-error">
          {{ error }}
        </div>

        <div v-else-if="result" class="mp-body">
          <div class="mp-bars">
            <div class="mp-bar-row">
              <span class="mp-label">{{ result.homeTeam }}</span>
              <div class="mp-bar-wrap">
                <div class="mp-bar home" :style="{ width: result.homeWinPercent + '%' }" />
              </div>
              <span class="mp-pct">{{ result.homeWinPercent }}%</span>
            </div>
            <div class="mp-bar-row">
              <span class="mp-label">Beraberlik</span>
              <div class="mp-bar-wrap">
                <div class="mp-bar draw" :style="{ width: result.drawPercent + '%' }" />
              </div>
              <span class="mp-pct">{{ result.drawPercent }}%</span>
            </div>
            <div class="mp-bar-row">
              <span class="mp-label">{{ result.awayTeam }}</span>
              <div class="mp-bar-wrap">
                <div class="mp-bar away" :style="{ width: result.awayWinPercent + '%' }" />
              </div>
              <span class="mp-pct">{{ result.awayWinPercent }}%</span>
            </div>
          </div>
          <p class="mp-analysis">{{ result.analysis }}</p>
          <p class="mp-disclaimer">Bu çıktı genel bilgilere dayalı tahmindir; gerçek maç sonucunu garanti etmez.</p>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
defineProps({
  open:       { type: Boolean, default: false },
  loading:    { type: Boolean, default: false },
  error:      { type: String, default: '' },
  result:     { type: Object, default: null },
  homeTeam:   { type: String, default: '' },
  awayTeam:   { type: String, default: '' }
})

defineEmits(['close'])
</script>

<style scoped>
.mp-overlay {
  position: fixed;
  inset: 0;
  z-index: 10050;
  background: rgba(0, 0, 0, 0.65);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.mp-modal {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 12px;
  max-width: 480px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 16px 48px rgba(0, 0, 0, 0.45);
}

.mp-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.1rem;
  border-bottom: 1px solid #21262d;
}

.mp-head h2 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 600;
  color: #e6edf3;
}

.mp-close {
  background: none;
  border: none;
  color: #8b949e;
  font-size: 1.5rem;
  line-height: 1;
  cursor: pointer;
  padding: 0.2rem;
}
.mp-close:hover { color: #e6edf3; }

.mp-sub {
  margin: 0;
  padding: 0.6rem 1.1rem 0;
  font-size: 0.9rem;
  color: #8b949e;
}
.mp-vs { color: #6e7681; margin: 0 0.25rem; }

.mp-body {
  padding: 1rem 1.1rem 1.25rem;
}

.mp-loading {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: #8b949e;
  font-size: 0.9rem;
}

.mp-spinner {
  width: 22px;
  height: 22px;
  border: 2px solid #30363d;
  border-top-color: #58a6ff;
  border-radius: 50%;
  animation: mp-spin 0.8s linear infinite;
}

@keyframes mp-spin {
  to { transform: rotate(360deg); }
}

.mp-error {
  color: #f85149;
  font-size: 0.9rem;
}

.mp-bars {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  margin-bottom: 1rem;
}

.mp-bar-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 1fr 40px;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.82rem;
}

.mp-label {
  color: #c9d1d9;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.mp-bar-wrap {
  height: 8px;
  background: #21262d;
  border-radius: 4px;
  overflow: hidden;
}

.mp-bar {
  height: 100%;
  border-radius: 4px;
  min-width: 2px;
  transition: width 0.4s ease;
}
.mp-bar.home { background: linear-gradient(90deg, #27AE60, #2ECC71); }
.mp-bar.draw { background: #8b949e; }
.mp-bar.away { background: linear-gradient(90deg, #a371f7, #8957e5); }

.mp-pct {
  text-align: right;
  font-weight: 600;
  color: #e6edf3;
  font-variant-numeric: tabular-nums;
}

.mp-analysis {
  margin: 0;
  font-size: 0.88rem;
  line-height: 1.55;
  color: #c9d1d9;
  white-space: pre-wrap;
}

.mp-disclaimer {
  margin: 0.85rem 0 0;
  font-size: 0.72rem;
  color: #6e7681;
  line-height: 1.4;
}
</style>
