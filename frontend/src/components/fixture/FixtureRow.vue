<template>
  <div
    :class="['fixture-row', { 'is-live': isLive, 'is-highlight': highlight, 'is-clickable': canGoToDetail }]"
    :role="canGoToDetail ? 'button' : undefined"
    :tabindex="canGoToDetail ? 0 : undefined"
    @click="goToDetail"
    @keydown.enter.prevent="canGoToDetail && goToDetail()"
    @keydown.space.prevent="canGoToDetail && goToDetail()"
  >

    <!-- Tarih / Saat -->
    <div class="match-date">
      <span class="date-day">{{ dateDay }}</span>
      <span class="date-time">{{ dateTime }}</span>
    </div>

    <!-- Durum etiketi -->
    <div class="match-status">
      <span :class="['status-badge', statusClass]">{{ statusLabel }}</span>
      <span v-if="isLive && match.minute" class="live-minute">{{ match.minute }}'</span>
    </div>

    <!-- Ev sahibi -->
    <div
      class="team home-team"
      :class="{ 'team-clickable': !!match.homeTeamId }"
      @click.stop="match.homeTeamId && goToTeam(match.homeTeamId)"
    >
      <span class="team-name">{{ match.homeTeam }}</span>
      <img
        v-if="match.homeTeamLogo"
        :src="match.homeTeamLogo"
        class="team-logo"
        @error="e => e.target.style.display='none'"
      />
    </div>

    <!-- Skor -->
    <div :class="['score-box', { 'score-live': isLive }]">
      <template v-if="hasScore">
        <span class="score">{{ match.homeScore }}</span>
        <span class="score-sep">–</span>
        <span class="score">{{ match.awayScore }}</span>
      </template>
      <span v-else class="score-upcoming">vs</span>
    </div>

    <!-- Deplasman -->
    <div
      class="team away-team"
      :class="{ 'team-clickable': !!match.awayTeamId }"
      @click.stop="match.awayTeamId && goToTeam(match.awayTeamId)"
    >
      <img
        v-if="match.awayTeamLogo"
        :src="match.awayTeamLogo"
        class="team-logo"
        @error="e => e.target.style.display='none'"
      />
      <span class="team-name">{{ match.awayTeam }}</span>
    </div>

    <!-- Lig -->
    <div class="match-league">
      <img
        v-if="match.leagueFlag"
        :src="match.leagueFlag"
        class="league-flag"
        @error="e => e.target.style.display='none'"
      />
      <span class="league-name">{{ match.league }}</span>
    </div>

    <!-- Oynanacak maçlar: AI önizleme (ana sayfa ile aynı API) -->
    <div class="fixture-actions" @click.stop>
      <button
        v-if="authStore.isAuthenticated && match.status === 0"
        type="button"
        class="ai-preview-btn"
        title="Yapay zeka yorumu"
        aria-label="Yapay zeka yorumu"
        @click="openAiPreview"
      >🤖</button>
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
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { fetchMatchPreview } from '../../api/matchPreviewApi'
import MatchAiPreviewModal from '../match/MatchAiPreviewModal.vue'

const props = defineProps({
  match:     { type: Object, required: true },
  highlight: { type: Boolean, default: false },
  /** Fikstür sayfası spor seçimi */
  sport:     { type: String, default: 'football' }
})

const router = useRouter()
const authStore = useAuthStore()

const goToTeam = (teamId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/team/${teamId}`)
}

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
      leagueName: props.match.league || null,
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

/** Fikstür API’sinden gelen maçların `Football-{id}` vb. id’si detay sayfasıyla uyumlu */
const canGoToDetail = computed(() => !!props.match?.id)

const goToDetail = () => {
  if (!canGoToDetail.value) return
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/match/${props.match.id}`)
}

// status: 0=NotStarted, 1=Live, 2=HalfTime, 3=Finished
const isLive     = computed(() => props.match.status === 1 || props.match.status === 2)
const hasScore   = computed(() => props.match.status !== 0)

const statusLabel = computed(() => {
  switch (props.match.status) {
    case 0: return 'Oynanacak'
    case 1: return 'Canlı'
    case 2: return 'HT'
    case 3: return 'Bitti'
    default: return '-'
  }
})

const statusClass = computed(() => ({
  'badge-upcoming':  props.match.status === 0,
  'badge-live':      props.match.status === 1,
  'badge-halftime':  props.match.status === 2,
  'badge-finished':  props.match.status === 3,
}))

const dateDay = computed(() => {
  const d = new Date(props.match.startTime)
  return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })
})

const dateTime = computed(() => {
  const d = new Date(props.match.startTime)
  return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
})
</script>

<style scoped>
.fixture-row {
  display: grid;
  grid-template-columns: 70px 80px 1fr 80px 1fr minmax(96px, 120px) 36px;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  background: #0d1117;
  border: 1px solid #21262d;
  border-radius: 8px;
  transition: background .15s;
}
.fixture-row:hover { background: #161b22; }
.fixture-row.is-clickable {
  cursor: pointer;
}
.fixture-row.is-clickable:hover {
  background: #1c2129;
  border-color: #30363d;
}
.fixture-row.is-live { border-color: #da3633; }
.fixture-row.is-highlight { background: #1a0a0a; }

/* Tarih */
.match-date {
  display: flex;
  flex-direction: column;
  align-items: center;
  line-height: 1.3;
}
.date-day  { font-size: 12px; font-weight: 600; color: #e6edf3; }
.date-time { font-size: 11px; color: #8b949e; }

/* Durum */
.match-status {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}
.status-badge {
  font-size: 10px;
  font-weight: 600;
  padding: 2px 7px;
  border-radius: 10px;
  text-transform: uppercase;
  letter-spacing: .4px;
}
.badge-upcoming { background: #1f3a5f; color: #58a6ff; }
.badge-live     { background: #3d0a0a; color: #f85149; animation: pulse-bg 1.4s infinite; }
.badge-finished { background: #0f2a0f; color: #2ECC71; }
.badge-halftime { background: #2d2200; color: #d29922; }
.live-minute    { font-size: 11px; color: #f85149; font-weight: 700; }

@keyframes pulse-bg {
  0%, 100% { opacity: 1; }
  50%       { opacity: .7; }
}

/* Takımlar */
.team {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}
.home-team { justify-content: flex-end; }
.away-team { justify-content: flex-start; }
.team-clickable {
  cursor: pointer;
  border-radius: 4px;
  padding: 2px 4px;
  margin: -2px -4px;
  transition: background 0.15s;
}
.team-clickable:hover { background: #30363d; }
.team-logo {
  width: 24px;
  height: 24px;
  object-fit: contain;
  flex-shrink: 0;
}
.team-name {
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  color: #e6edf3;
}

/* Skor */
.score-box {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 6px;
  padding: 5px 8px;
  min-width: 64px;
}
.score-box.score-live { border-color: #da3633; }
.score     { font-size: 15px; font-weight: 700; color: #e6edf3; }
.score-sep { font-size: 14px; color: #8b949e; }
.score-upcoming { font-size: 13px; color: #8b949e; }

/* Lig */
.match-league {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  justify-content: flex-end;
}
.league-flag {
  width: 18px;
  height: 12px;
  object-fit: cover;
  border-radius: 2px;
  flex-shrink: 0;
}
.league-name {
  font-size: 11px;
  color: #8b949e;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.fixture-actions {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 0;
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

/* Responsive */
@media (max-width: 700px) {
  .fixture-row {
    grid-template-columns: 60px 70px 1fr 60px 1fr 36px;
  }
  .match-league { display: none; }
}
@media (max-width: 480px) {
  .fixture-row {
    grid-template-columns: 1fr 60px 1fr 36px;
    grid-template-rows: auto auto;
  }
  .match-date, .match-status { display: none; }
  .match-league { display: none; }
}
</style>
