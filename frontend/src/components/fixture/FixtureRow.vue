<template>
  <div :class="['fixture-row', { 'is-live': isLive, 'is-highlight': highlight }]">

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
    <div class="team home-team">
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
    <div class="team away-team">
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

  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  match:     { type: Object, required: true },
  highlight: { type: Boolean, default: false }
})

// status: 0=NotStarted, 1=Live, 2=Finished, 3=HalfTime
const isLive     = computed(() => props.match.status === 1 || props.match.status === 3)
const hasScore   = computed(() => props.match.status !== 0)

const statusLabel = computed(() => {
  switch (props.match.status) {
    case 0: return 'Oynanacak'
    case 1: return 'Canlı'
    case 2: return 'Bitti'
    case 3: return 'HT'
    default: return '-'
  }
})

const statusClass = computed(() => ({
  'badge-upcoming':  props.match.status === 0,
  'badge-live':      props.match.status === 1,
  'badge-finished':  props.match.status === 2,
  'badge-halftime':  props.match.status === 3,
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
  grid-template-columns: 70px 80px 1fr 80px 1fr 130px;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  background: #0d1117;
  border: 1px solid #21262d;
  border-radius: 8px;
  transition: background .15s;
}
.fixture-row:hover { background: #161b22; }
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
.badge-finished { background: #0f2a0f; color: #3fb950; }
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

/* Responsive */
@media (max-width: 700px) {
  .fixture-row {
    grid-template-columns: 60px 70px 1fr 60px 1fr;
  }
  .match-league { display: none; }
}
@media (max-width: 480px) {
  .fixture-row {
    grid-template-columns: 1fr 60px 1fr;
    grid-template-rows: auto auto;
  }
  .match-date, .match-status { display: none; }
  .match-league { display: none; }
}
</style>
