<template>
  <div class="match-row" @click="$emit('click', match.id)">
    <!-- Maç saati / durumu -->
    <div class="match-time-col">
      <template v-if="match.status === 1">
        <span class="live-indicator"></span>
        <span class="match-minute">{{ match.minute }}'</span>
      </template>
      <template v-else-if="match.status === 2">
        <span class="status-badge halftime">IY</span>
      </template>
      <template v-else-if="match.status === 3">
        <span class="status-badge finished">BT</span>
      </template>
      <template v-else>
        <span class="match-clock">{{ formatTime(match.startTime) }}</span>
      </template>
    </div>

    <!-- Takımlar ve skor -->
    <div class="match-teams-col">
      <div v-for="(side, i) in [
        { team: match.homeTeam, logo: match.homeTeamLogo, score: match.homeScore, won: match.status === 3 && match.homeScore > match.awayScore },
        { team: match.awayTeam, logo: match.awayTeamLogo, score: match.awayScore, won: match.status === 3 && match.awayScore > match.homeScore }
      ]" :key="i" class="team-row">
        <div class="team-info">
          <img
            v-if="side.logo"
            :src="side.logo"
            :alt="side.team"
            class="team-logo-sm"
            loading="lazy"
            @error="$event.target.style.display='none'"
          />
          <span v-else class="team-logo-placeholder">{{ side.team?.charAt(0) }}</span>
          <span class="team-name" :class="{ winner: side.won }">{{ side.team }}</span>
        </div>
        <span class="team-score" :class="{ 'is-live': match.status === 1 }">{{ side.score }}</span>
      </div>
    </div>

    <!-- Favori + oynanacak maçlar için AI önizleme (sohbet panelinden bağımsız) -->
    <div class="match-actions-col">
      <button
        v-if="isAuthenticated && match.status === 0"
        type="button"
        class="ai-preview-btn"
        title="Yapay zeka yorumu"
        aria-label="Yapay zeka yorumu"
        @click.stop="openAiPreview"
      >🤖</button>
      <button
        v-if="isAuthenticated"
        class="fav-btn"
        :class="{ active: isFavorite }"
        @click.stop="$emit('toggle-favorite', match.id)"
        title="Favorilere ekle / çıkar"
      >★</button>
    </div>

    <MatchAiPreviewModal
      :open="aiOpen"
      :loading="aiLoading"
      :error="aiError"
      :result="aiResult"
      :home-team="match.homeTeam"
      :away-team="match.awayTeam"
      @close="closeAiPreview"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useFormatters } from '../../composables/useFormatters'
import { fetchMatchPreview } from '../../api/matchPreviewApi'
import MatchAiPreviewModal from './MatchAiPreviewModal.vue'

const { formatTime } = useFormatters()

const props = defineProps({
  match:           { type: Object,  required: true },
  leagueName:      { type: String,  default: '' },
  sport:           { type: String,  default: 'football' },
  isFavorite:      { type: Boolean, default: false },
  isAuthenticated: { type: Boolean, default: false }
})

defineEmits(['click', 'toggle-favorite'])

const aiOpen = ref(false)
const aiLoading = ref(false)
const aiError = ref('')
const aiResult = ref(null)

const openAiPreview = async () => {
  aiOpen.value = true
  aiLoading.value = true
  aiError.value = ''
  aiResult.value = null
  try {
    const { data } = await fetchMatchPreview({
      homeTeam: props.match.homeTeam,
      awayTeam: props.match.awayTeam,
      leagueName: props.leagueName || null,
      sport: props.sport || 'football'
    })
    aiResult.value = data
  } catch (e) {
    const d = e.response?.data
    aiError.value =
      d?.message ||
      d?.detail ||
      d?.title ||
      (e.response?.status === 401 ? 'Oturum gerekli veya süresi doldu.' : null) ||
      'Analiz alınamadı. Daha sonra tekrar deneyin.'
  } finally {
    aiLoading.value = false
  }
}

const closeAiPreview = () => {
  aiOpen.value = false
}
</script>

<style scoped>
.match-row {
  display: flex;
  align-items: center;
  padding: 0.6rem 1.25rem;
  cursor: pointer;
  transition: background 0.15s;
  border-bottom: 1px solid #21262d10;
}
.match-row:hover { background: #1c2129; }
.match-row:last-child { border-bottom: none; }

.match-time-col {
  width: 60px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.15rem;
  flex-shrink: 0;
}

.live-indicator {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #f85149;
  animation: pulse 1.5s infinite;
}

@keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }

.match-minute { font-size: 0.8rem; font-weight: 700; color: #f85149; }
.match-clock  { font-size: 0.8rem; color: #8b949e; font-weight: 500; }

.status-badge {
  font-size: 0.7rem;
  font-weight: 600;
  padding: 0.15rem 0.4rem;
  border-radius: 3px;
}
.status-badge.halftime { background: #da3633; color: #ffffff; }
.status-badge.finished { color: #8b949e; background: #21262d; }

.match-teams-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.team-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.team-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  flex: 1;
}

.team-logo-sm {
  width: 18px;
  height: 18px;
  object-fit: contain;
  flex-shrink: 0;
  border-radius: 2px;
}

.team-logo-placeholder {
  width: 18px;
  height: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #21262d;
  border-radius: 3px;
  font-size: 0.65rem;
  font-weight: 700;
  color: #8b949e;
  flex-shrink: 0;
}

.team-name {
  font-size: 0.85rem;
  font-weight: 400;
  color: #c9d1d9;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.team-name.winner { font-weight: 700; color: #ffffff; }

.team-score {
  font-size: 0.85rem;
  font-weight: 700;
  color: #e1e4e8;
  min-width: 18px;
  text-align: center;
}
.team-score.is-live { color: #f85149; }

.match-actions-col {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.25rem;
  flex-shrink: 0;
  min-width: 56px;
}

.ai-preview-btn {
  background: #21262d;
  border: 1px solid #30363d;
  color: #58a6ff;
  font-size: 0.85rem;
  font-weight: 700;
  letter-spacing: 0;
  padding: 0.2rem 0.35rem;
  border-radius: 4px;
  cursor: pointer;
  line-height: 1.2;
}
.ai-preview-btn:hover {
  background: #30363d;
  border-color: #58a6ff;
}

.fav-btn {
  background: none;
  border: none;
  color: #30363d;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.2s;
  padding: 0.25rem;
}
.fav-btn:hover  { color: #e3b341; transform: scale(1.15); }
.fav-btn.active { color: #e3b341; text-shadow: 0 0 8px #e3b34166; }

@media (max-width: 600px) {
  .match-row { padding: 0.5rem 0.75rem; }
}
</style>
