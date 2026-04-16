<template>
  <aside class="right-panel">

    <!-- Lig seçilmemişse → Popüler lig hızlı seçimi -->
    <div v-if="!leagueInfo" class="standings-placeholder">

      <!-- Başlık -->
      <div class="ph-header">
        <svg class="ph-header-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M3 3h18v4H3V3zm2 6h4v12H5V9zm6 0h4v8h-4V9zm6 0h4v5h-4V9z" fill="currentColor" opacity=".15"/>
          <rect x="3" y="3" width="18" height="4" rx="1" stroke="currentColor" stroke-width="1.5"/>
          <rect x="5" y="9" width="4" height="12" rx="1" stroke="currentColor" stroke-width="1.5"/>
          <rect x="11" y="9" width="4" height="8" rx="1" stroke="currentColor" stroke-width="1.5"/>
          <rect x="17" y="9" width="4" height="5" rx="1" stroke="currentColor" stroke-width="1.5"/>
        </svg>
        <div>
          <p class="ph-title">Puan Tablosu</p>
          <p class="ph-sub">Bir lig seçerek tabloyu görüntüleyin</p>
        </div>
      </div>

      <!-- Turnuvalar (sadece futbol) -->
      <div v-if="sport === 'football' && tournamentLeagues.length" class="ph-leagues">
        <p class="ph-section-label">
          <svg class="ph-label-icon" viewBox="0 0 16 16" fill="none">
            <path d="M8 1l1.5 3.5L13 5l-2.5 2.5.5 3.5L8 9.5 5 11l.5-3.5L3 5l3.5-.5L8 1z" stroke="currentColor" stroke-width="1.2" stroke-linejoin="round"/>
          </svg>
          Avrupa &amp; Milli Takım
        </p>
        <div class="ph-league-list">
          <button
            v-for="league in tournamentLeagues"
            :key="league.key"
            class="ph-league-btn ph-league-btn--tournament"
            @click="$emit('select-league', league)"
          >
            <div class="ph-league-left">
              <div class="ph-flag-wrap ph-flag-wrap--tournament">
                <img
                  v-if="league.flag"
                  :src="league.flag"
                  class="ph-flag ph-flag--logo"
                  @error="$event.target.style.display='none'"
                />
                <span v-else class="ph-flag-fallback">🏆</span>
              </div>
              <span class="ph-league-name">{{ league.displayName || league.name }}</span>
            </div>
            <svg class="ph-arrow" viewBox="0 0 16 16" fill="none">
              <path d="M6 4l4 4-4 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- Popüler Ligler -->
      <div v-if="popularLeagues.length" class="ph-leagues">
        <p class="ph-section-label">
          <svg class="ph-label-icon" viewBox="0 0 16 16" fill="none">
            <circle cx="8" cy="8" r="6" stroke="currentColor" stroke-width="1.2"/>
            <path d="M8 5v3l2 1.5" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/>
          </svg>
          Popüler Ligler
        </p>
        <div class="ph-league-list">
          <button
            v-for="league in popularLeagues"
            :key="league.key"
            class="ph-league-btn"
            @click="$emit('select-league', league)"
          >
            <div class="ph-league-left">
              <div class="ph-flag-wrap">
                <img
                  v-if="league.flag"
                  :src="league.flag"
                  class="ph-flag"
                  @error="$event.target.style.display='none'"
                />
                <span v-else class="ph-flag-fallback">🏆</span>
              </div>
              <span class="ph-league-name">{{ league.displayName || league.name }}</span>
            </div>
            <svg class="ph-arrow" viewBox="0 0 16 16" fill="none">
              <path d="M6 4l4 4-4 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </button>
        </div>
      </div>

    </div>

    <!-- Lig seçiliyse -->
    <div v-else class="standings-panel">
      <!-- Başlık -->
      <div class="standings-header">
        <button class="back-btn" @click="$emit('select-league', null)" title="Geri">←</button>
        <img
          v-if="leagueInfo.flag"
          :src="leagueInfo.flag"
          class="standings-flag"
          @error="$event.target.style.display='none'"
        />
        <span v-else class="standings-league-icon">🏆</span>
        <h3 class="standings-title">{{ leagueInfo.name }}</h3>
      </div>

      <!-- Yükleniyor -->
      <LoadingSpinner v-if="isLoading" :small="true" label="Puan tablosu yükleniyor..." />

      <!-- Veri yok -->
      <div v-else-if="standings.length === 0" class="standings-empty">
        <p>Bu lig için puan tablosu bulunamadı</p>
        <small>Bu lig henüz desteklenmiyor olabilir</small>
      </div>

      <!-- Tablo -->
      <table v-else class="standings-table">
        <thead>
          <tr>
            <th class="col-pos">#</th>
            <th class="col-team">Takım</th>
            <th class="col-stat">O</th>
            <th class="col-stat">G</th>
            <th v-if="sport === 'football'" class="col-stat">B</th>
            <th class="col-stat">M</th>
            <th class="col-stat" :title="goalsForTitle">{{ goalsForLabel }}</th>
            <th class="col-stat" :title="goalsAgainstTitle">{{ goalsAgainstLabel }}</th>
            <th class="col-stat">AV</th>
            <th class="col-pts">P</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(team, index) in standings"
            :key="team.name"
            :class="rowClass(index)"
          >
            <td class="col-pos">{{ index + 1 }}</td>
            <td class="col-team">
              <div
                class="standings-team-cell"
                :class="{ 'team-clickable': !!team.teamId }"
                @click="team.teamId && goToTeam(team.teamId)"
              >
                <img
                  v-if="team.logo"
                  :src="team.logo"
                  :alt="team.name"
                  class="standings-team-logo"
                  loading="lazy"
                  @error="$event.target.style.display='none'"
                />
                <span class="standings-team-name">{{ team.name }}</span>
              </div>
            </td>
            <td class="col-stat">{{ team.played }}</td>
            <td class="col-stat">{{ team.won }}</td>
            <td v-if="sport === 'football'" class="col-stat">{{ team.drawn }}</td>
            <td class="col-stat">{{ team.lost }}</td>
            <td class="col-stat">{{ team.goalsFor }}</td>
            <td class="col-stat">{{ team.goalsAgainst }}</td>
            <td class="col-stat" :class="{ positive: team.goalDiff > 0, negative: team.goalDiff < 0 }">
              {{ team.goalDiff > 0 ? '+' : '' }}{{ team.goalDiff }}
            </td>
            <td class="col-pts">{{ team.points }}</td>
          </tr>
        </tbody>
      </table>

      <!-- Renk açıklamaları -->
      <div v-if="sport === 'football' && standings.length > 5" class="standings-legend">
        <div class="legend-item"><span class="legend-color champ"></span> Şampiyonlar Ligi</div>
        <div class="legend-item"><span class="legend-color europa"></span> Avrupa Ligi</div>
        <div class="legend-item"><span class="legend-color relegation"></span> Düşme hattı</div>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import LoadingSpinner from '../common/LoadingSpinner.vue'
import { POPULAR_STANDINGS_LEAGUES } from '../../constants/sports'

const router    = useRouter()
const authStore = useAuthStore()

const goToTeam = (teamId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/team/${teamId}`)
}

const props = defineProps({
  leagueInfo: { type: Object,  default: null },
  standings:  { type: Array,   default: () => [] },
  isLoading:  { type: Boolean, default: false },
  sport:      { type: String,  default: 'football' }
})

defineEmits(['select-league'])

const TOURNAMENT_KEYS = new Set([
  'World::UEFA Champions League',
  'World::UEFA Europa League',
  'World::UEFA Europa Conference League',
  'World::UEFA Nations League',
  'World::FIFA World Cup',
  'World::UEFA Euro',
  'World::World Cup - Qualification Europe',
  'World::Copa America',
  'World::Africa Cup of Nations',
])

const allLeagues       = computed(() => POPULAR_STANDINGS_LEAGUES[props.sport] ?? [])
const popularLeagues   = computed(() => allLeagues.value.filter(l => !TOURNAMENT_KEYS.has(l.key)))
const tournamentLeagues = computed(() => allLeagues.value.filter(l => TOURNAMENT_KEYS.has(l.key)))

const goalsForLabel     = computed(() => props.sport === 'volleyball' ? 'SG' : 'A')
const goalsAgainstLabel = computed(() => props.sport === 'volleyball' ? 'SY' : 'Y')
const goalsForTitle     = computed(() => props.sport === 'volleyball' ? 'Set Galibiyet' : props.sport === 'basketball' ? 'Atılan Puan' : 'Atılan Gol')
const goalsAgainstTitle = computed(() => props.sport === 'volleyball' ? 'Set Yenilgi'   : props.sport === 'basketball' ? 'Yenilen Puan' : 'Yenilen Gol')

const rowClass = (index) => {
  const total = props.standings.length
  if (total <= 3) return ''
  if (index < 1) return 'row-champion'
  if (index < 3) return 'row-europa'
  if (index >= total - 2) return 'row-relegation'
  return ''
}
</script>

<style scoped>
.right-panel {
  background: #0d1117;
  overflow-y: auto;
  max-height: calc(100vh - 112px);
  position: sticky;
  top: 112px;
}

/* ── Placeholder ── */
.standings-placeholder {
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* Tüm satırlarda tek yatay padding değeri — sayfanın geri kalanıyla hizalı */
.ph-header,
.ph-section-label,
.ph-league-btn {
  padding-left: 1.25rem;
  padding-right: 1.25rem;
}

/* Başlık */
.ph-header {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding-top: 0.85rem;
  padding-bottom: 0.85rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.ph-header-icon {
  width: 30px;
  height: 30px;
  color: #58a6ff;
  flex-shrink: 0;
  opacity: 0.9;
}

.ph-title {
  font-size: 0.9rem;
  font-weight: 700;
  color: #e6edf3;
  margin: 0 0 2px;
  letter-spacing: 0.1px;
}

.ph-sub {
  font-size: 0.71rem;
  color: #484f58;
  margin: 0;
  line-height: 1.3;
}

/* Liste alanı */
.ph-leagues {
  flex: 1;
  overflow-y: auto;
}

.ph-section-label {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.63rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.9px;
  color: #484f58;
  padding-top: 0.75rem;
  padding-bottom: 0.35rem;
  margin: 0;
}

.ph-label-icon {
  width: 11px;
  height: 11px;
  flex-shrink: 0;
  color: #484f58;
}

/* Lig satırları */
.ph-league-list { display: flex; flex-direction: column; }

.ph-league-btn {
  display: flex;
  align-items: center;
  width: 100%;
  padding-top: 0.6rem;
  padding-bottom: 0.6rem;
  background: transparent;
  border: none;
  border-bottom: 1px solid #21262d40;
  cursor: pointer;
  transition: background 0.13s;
  text-align: left;
  gap: 0;
}

.ph-league-btn:hover { background: #161b22; }
.ph-league-btn:hover .ph-league-name { color: #ffffff; }
.ph-league-btn:hover .ph-arrow { color: #58a6ff; opacity: 1; }

/* Sol grup: bayrak + isim */
.ph-league-left {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 0;
}

/* Bayrak: sabit 24×17 kutu, her bayrak/ikon aynı hizada */
.ph-flag-wrap {
  width: 24px;
  height: 17px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.ph-flag {
  width: 24px;
  height: 17px;
  object-fit: cover;
  border-radius: 2px;
  display: block;
}

.ph-flag-fallback {
  font-size: 0.875rem;
  line-height: 1;
  opacity: 0.7;
}

/* Turnuva logoları kare/yuvarlak */
.ph-flag-wrap--tournament {
  width: 22px;
  height: 22px;
}

.ph-flag--logo {
  width: 22px;
  height: 22px;
  object-fit: contain;
  border-radius: 3px;
}

/* Turnuva satırları — hafif mavi ton */
.ph-league-btn--tournament:hover {
  background: #0d2137;
}
.ph-league-btn--tournament:hover .ph-league-name {
  color: #79c0ff;
}
.ph-league-btn--tournament:hover .ph-arrow {
  color: #58a6ff;
}

.ph-league-name {
  font-size: 0.79rem;
  font-weight: 500;
  color: #c9d1d9;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  transition: color 0.13s;
}

/* Ok ikonu: sabit genişlik, sağda hizalı */
.ph-arrow {
  width: 15px;
  height: 15px;
  flex-shrink: 0;
  color: #30363d;
  opacity: 0.5;
  transition: color 0.13s, opacity 0.13s;
}

/* ── Panel başlık ── */
.standings-panel { padding: 0; }

.standings-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.back-btn {
  background: none;
  border: none;
  color: #8b949e;
  font-size: 1rem;
  cursor: pointer;
  padding: 0.1rem 0.3rem;
  border-radius: 4px;
  transition: all 0.15s;
  flex-shrink: 0;
}
.back-btn:hover { color: #c9d1d9; background: #21262d; }

.standings-league-icon { font-size: 1rem; }

.standings-flag {
  width: 22px;
  height: 16px;
  object-fit: cover;
  border-radius: 2px;
  flex-shrink: 0;
}

.standings-title { font-size: 0.85rem; font-weight: 700; color: #ffffff; margin: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.standings-empty {
  padding: 2rem 1.25rem;
  text-align: center;
  color: #484f58;
  font-size: 0.85rem;
}
.standings-empty small { display: block; margin-top: 0.35rem; font-size: 0.75rem; }

/* ── Tablo ── */
.standings-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.78rem;
}

.standings-table thead {
  background: #161b22;
  position: sticky;
  top: 0;
}

/* th ve td aynı dikey padding — hizalama tutarlı */
.standings-table th,
.standings-table td {
  padding: 0.6rem 0.4rem;
  text-align: center;
}

.standings-table th {
  font-weight: 600;
  color: #8b949e;
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  border-bottom: 1px solid #21262d;
}

.standings-table td {
  border-bottom: 1px solid #21262d15;
  color: #c9d1d9;
}

.standings-table tbody tr { transition: background 0.15s; }
.standings-table tbody tr:hover { background: #161b22; }

/* Kolon genişlikleri ve hizaları */
.col-pos  { width: 28px; font-weight: 600; color: #8b949e; }
.col-stat { width: 28px; color: #8b949e; }
.col-pts  { width: 32px; font-weight: 700; color: #ffffff; }

/* Takım sütunu: sola hizalı, taşmayı kırp */
.standings-table th.col-team,
.standings-table td.col-team {
  text-align: left;
  padding-left: 0.5rem;
  font-weight: 500;
  max-width: 130px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.standings-team-cell {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}
.team-clickable {
  cursor: pointer;
  border-radius: 6px;
  transition: background 0.13s;
}
.team-clickable:hover {
  background: #21262d;
}
.team-clickable:hover .standings-team-name {
  color: #58a6ff;
}

.standings-team-logo {
  width: 16px;
  height: 16px;
  object-fit: contain;
  flex-shrink: 0;
  border-radius: 2px;
}

.standings-team-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Av farkı renkleri — kendi sınıflarından geliyor, !important gereksiz */
.positive { color: #3fb950; }
.negative { color: #f85149; }

.row-champion   { border-left: 3px solid #58a6ff; }
.row-champion .col-pos   { color: #58a6ff; }
.row-europa     { border-left: 3px solid #f0883e; }
.row-europa .col-pos     { color: #f0883e; }
.row-relegation { border-left: 3px solid #f85149; }
.row-relegation .col-pos { color: #f85149; }

.standings-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 0.85rem 1.25rem;
  border-top: 1px solid #21262d;
}

.legend-item   { display: flex; align-items: center; gap: 0.35rem; font-size: 0.7rem; color: #8b949e; }
.legend-color  { width: 10px; height: 10px; border-radius: 2px; }
.legend-color.champ      { background: #58a6ff; }
.legend-color.europa     { background: #f0883e; }
.legend-color.relegation { background: #f85149; }

@media (max-width: 900px) {
  .right-panel { position: static; max-height: none; border-top: 1px solid #21262d; }
}
</style>
