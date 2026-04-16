<template>
  <div class="detail-page">

    <!-- YÜKLENİYOR DURUMU -->
    <div v-if="isLoading" class="state-container">
      <div class="spinner"></div>
      <span class="state-text">Maç yükleniyor...</span>
    </div>

    <!-- HATA DURUMU -->
    <div v-else-if="errorMessage || !match" class="state-container">
      <span class="state-icon">⚠️</span>
      <span class="state-text">{{ errorMessage || 'Maç bulunamadı.' }}</span>
      <button class="btn-go-home" @click="goHome">Ana Sayfaya Dön</button>
    </div>

    <!-- MAÇ İÇERİĞİ -->
    <template v-else>

      <!-- SKOR BÖLÜMÜ -->
      <div class="score-section">
        <!-- Lig bilgisi -->
        <div class="league-badge">
          <span class="league-badge-icon">🏆</span>
          <span class="league-badge-text">{{ match.league || 'Bilinmeyen Lig' }}</span>
        </div>

        <!-- Takımlar ve skor -->
        <div class="scoreboard">
          <!-- Ev sahibi takım -->
          <div
            class="team-block"
            :class="{ 'team-clickable': !!match.homeTeamId }"
            @click="match.homeTeamId && goToTeam(match.homeTeamId)"
          >
            <div class="team-emblem" :class="{ 'has-logo': match.homeTeamLogo }">
              <img
                v-if="match.homeTeamLogo"
                :src="match.homeTeamLogo"
                :alt="match.homeTeam"
                class="emblem-img"
                @error="$event.target.style.display='none'; $event.target.nextElementSibling.style.display='flex'"
              />
              <span class="emblem-letter" :style="match.homeTeamLogo ? { display: 'none' } : {}">{{ getTeamInitial(match.homeTeam) }}</span>
            </div>
            <span class="team-label">{{ match.homeTeam }}</span>
          </div>

          <!-- Skor paneli -->
          <div class="score-center">
            <div class="score-digits">
              <span class="digit home" :class="{ winner: match.status === 3 && match.homeScore > match.awayScore }">
                {{ match.homeScore }}
              </span>
              <span class="score-separator">-</span>
              <span class="digit away" :class="{ winner: match.status === 3 && match.awayScore > match.homeScore }">
                {{ match.awayScore }}
              </span>
            </div>

            <!-- Maç durumu -->
            <div v-if="match.status === 1" class="match-status live">
              <span class="live-pulse"></span>
              <span>CANLI {{ match.minute }}'</span>
            </div>
            <div v-else-if="match.status === 2" class="match-status halftime">
              Devre Arası
            </div>
            <div v-else-if="match.status === 3" class="match-status finished">
              Maç Bitti
            </div>
            <div v-else class="match-status upcoming">
              {{ formatDateTime(match.startTime) }}
            </div>
          </div>

          <!-- Deplasman takımı -->
          <div
            class="team-block"
            :class="{ 'team-clickable': !!match.awayTeamId }"
            @click="match.awayTeamId && goToTeam(match.awayTeamId)"
          >
            <div class="team-emblem" :class="{ 'has-logo': match.awayTeamLogo }">
              <img
                v-if="match.awayTeamLogo"
                :src="match.awayTeamLogo"
                :alt="match.awayTeam"
                class="emblem-img"
                @error="$event.target.style.display='none'; $event.target.nextElementSibling.style.display='flex'"
              />
              <span class="emblem-letter" :style="match.awayTeamLogo ? { display: 'none' } : {}">{{ getTeamInitial(match.awayTeam) }}</span>
            </div>
            <span class="team-label">{{ match.awayTeam }}</span>
          </div>
        </div>
      </div>

      <!-- SEKMELİ İÇERİK ALANI -->
      <div class="content-area">
        <div class="content-card">
          <!-- Sekmeler -->
          <div class="tab-bar">
            <button
              :class="['tab-item', { active: activeTab === 'stats' }]"
              @click="activeTab = 'stats'"
            >
              📊 İstatistikler
            </button>
            <button
              v-if="match.sportType === 0"
              :class="['tab-item', { active: activeTab === 'events' }]"
              @click="activeTab = 'events'"
            >
              ⚽ Olaylar
            </button>
            <button
              v-if="match.sportType === 0"
              :class="['tab-item', { active: activeTab === 'lineups' }]"
              @click="activeTab = 'lineups'"
            >
              👥 Kadrolar
            </button>
            <button
              :class="['tab-item', { active: activeTab === 'info' }]"
              @click="activeTab = 'info'"
            >
              ℹ️ Bilgiler
            </button>
            <button
              :class="['tab-item', { active: activeTab === 'chat' }]"
              @click="activeTab = 'chat'"
            >
              💬 Sohbet
            </button>
          </div>

          <!-- İSTATİSTİKLER SEKMESİ -->
          <div v-if="activeTab === 'stats'" class="tab-content">
            <!-- İstatistik yükleniyor -->
            <div v-if="isStatsLoading" class="stats-loading">
              <div class="spinner-sm"></div>
              <span>İstatistikler yükleniyor...</span>
            </div>

            <!-- İstatistik bulunamadı -->
            <div v-else-if="!hasStatistics" class="stats-empty">
              <span class="stats-empty-icon">📊</span>
              <p>İstatistik verisi bulunamadı</p>
              <small>Maç başlamadıysa veya veri henüz mevcut değilse istatistikler gösterilemez</small>
            </div>

            <!-- Basketbol / Voleybol: Periyot tablosu -->
            <template v-else-if="isPeriodicSport">
              <div class="period-stats">
                <table class="period-table">
                  <thead>
                    <tr>
                      <th class="period-team-col">Takım</th>
                      <th v-for="p in periodHeaders" :key="p.key" class="period-col">{{ p.label }}</th>
                      <th class="period-col period-total">T</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td class="period-team-col">
                        <div class="period-team-info">
                          <img v-if="match.homeTeamLogo" :src="match.homeTeamLogo" class="period-team-logo" />
                          <span>{{ match.homeTeam }}</span>
                        </div>
                      </td>
                      <td v-for="p in periodHeaders" :key="'h-'+p.key" class="period-col">
                        {{ match.statistics?.['home' + p.key] ?? '-' }}
                      </td>
                      <td class="period-col period-total">{{ match.homeScore }}</td>
                    </tr>
                    <tr>
                      <td class="period-team-col">
                        <div class="period-team-info">
                          <img v-if="match.awayTeamLogo" :src="match.awayTeamLogo" class="period-team-logo" />
                          <span>{{ match.awayTeam }}</span>
                        </div>
                      </td>
                      <td v-for="p in periodHeaders" :key="'a-'+p.key" class="period-col">
                        {{ match.statistics?.['away' + p.key] ?? '-' }}
                      </td>
                      <td class="period-col period-total">{{ match.awayScore }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>

            <!-- Futbol: Bar istatistikleri -->
            <template v-else>
              <div
                v-for="stat in matchStats"
                :key="stat.label"
                class="stat-row"
              >
                <span class="stat-home-val">{{ stat.home }}</span>
                <div class="stat-center">
                  <span class="stat-label">{{ stat.label }}</span>
                  <div class="stat-bar-wrapper">
                    <div class="stat-bar">
                      <div
                        class="stat-bar-fill home-fill"
                        :style="{ width: getBarPercent(stat.home, stat.away) + '%' }"
                      ></div>
                    </div>
                    <div class="stat-bar">
                      <div
                        class="stat-bar-fill away-fill"
                        :style="{ width: getBarPercent(stat.away, stat.home) + '%' }"
                      ></div>
                    </div>
                  </div>
                </div>
                <span class="stat-away-val">{{ stat.away }}</span>
              </div>
            </template>
          </div>

          <!-- OLAYLAR SEKMESİ (Futbol) -->
          <div v-if="activeTab === 'events'" class="tab-content">
            <div v-if="isStatsLoading" class="stats-loading">
              <div class="spinner-sm"></div>
              <span>Olaylar yükleniyor...</span>
            </div>

            <div v-else-if="!matchEvents.length" class="stats-empty">
              <span class="stats-empty-icon">⚽</span>
              <p>Olay verisi bulunamadı</p>
              <small>Maç başlamadıysa veya bu lig için olay verisi mevcut değilse gösterilemez</small>
            </div>

            <div v-else class="events-timeline">
              <div
                v-for="(event, index) in matchEvents"
                :key="index"
                class="event-item"
                :class="{ 'event-home': event.isHome, 'event-away': !event.isHome }"
              >
                <!-- Dakika -->
                <div class="event-minute">
                  <span>{{ event.minute }}'</span>
                  <span v-if="event.extraMinute" class="event-extra">+{{ event.extraMinute }}</span>
                </div>

                <!-- İkon -->
                <div class="event-icon" :class="event.iconClass">
                  {{ event.icon }}
                </div>

                <!-- Detay -->
                <div class="event-detail">
                  <span class="event-player">{{ event.playerName }}</span>
                  <span v-if="event.assistName" class="event-assist">
                    <template v-if="event.type === 'subst'">↔ {{ event.assistName }}</template>
                    <template v-else>({{ event.assistName }})</template>
                  </span>
                  <span class="event-type-label">{{ event.label }}</span>
                </div>

                <!-- Takım logosu -->
                <div class="event-team">
                  <img v-if="event.teamLogo" :src="event.teamLogo" class="event-team-logo" />
                </div>
              </div>
            </div>
          </div>

          <!-- KADROLAR SEKMESİ (Futbol — ilk 11) -->
          <div v-if="activeTab === 'lineups'" class="tab-content">
            <div v-if="isStatsLoading" class="stats-loading">
              <div class="spinner-sm"></div>
              <span>Kadrolar yükleniyor...</span>
            </div>

            <div v-else-if="!hasLineups" class="stats-empty">
              <span class="stats-empty-icon">👥</span>
              <p>Kadro verisi bulunamadı</p>
              <small>Maç öncesinde veya bu lig için kadro yayınlanmadıysa ilk 11 gösterilemez</small>
            </div>

            <div v-else class="lineups-wrap">
              <div class="lineups-columns">
                <div class="lineup-side lineup-side-home">
                  <div class="lineup-side-head">
                    <img
                      v-if="match.homeTeamLogo"
                      :src="match.homeTeamLogo"
                      alt=""
                      class="lineup-side-logo"
                    />
                    <div class="lineup-side-titles">
                      <span class="lineup-side-name">{{ match.homeTeam }}</span>
                      <span v-if="homeLineupFormation" class="lineup-formation">{{ homeLineupFormation }}</span>
                    </div>
                  </div>
                  <ol class="lineup-list">
                    <li
                      v-for="(p, idx) in homeStartingXI"
                      :key="'h-'+idx"
                      class="lineup-row"
                    >
                      <span class="lineup-num">{{ p.number != null ? p.number : '–' }}</span>
                      <span class="lineup-pos" :title="lineupPosTitle(p.position)">{{ formatLineupPos(p.position) }}</span>
                      <span class="lineup-player">{{ p.name }}</span>
                    </li>
                  </ol>
                </div>

                <div class="lineup-side lineup-side-away">
                  <div class="lineup-side-head">
                    <img
                      v-if="match.awayTeamLogo"
                      :src="match.awayTeamLogo"
                      alt=""
                      class="lineup-side-logo"
                    />
                    <div class="lineup-side-titles">
                      <span class="lineup-side-name">{{ match.awayTeam }}</span>
                      <span v-if="awayLineupFormation" class="lineup-formation">{{ awayLineupFormation }}</span>
                    </div>
                  </div>
                  <ol class="lineup-list">
                    <li
                      v-for="(p, idx) in awayStartingXI"
                      :key="'a-'+idx"
                      class="lineup-row"
                    >
                      <span class="lineup-num">{{ p.number != null ? p.number : '–' }}</span>
                      <span class="lineup-pos" :title="lineupPosTitle(p.position)">{{ formatLineupPos(p.position) }}</span>
                      <span class="lineup-player">{{ p.name }}</span>
                    </li>
                  </ol>
                </div>
              </div>
            </div>
          </div>

          <!-- BİLGİLER SEKMESİ -->
          <div v-if="activeTab === 'info'" class="tab-content">
            <div class="info-grid">
              <div class="info-item">
                <span class="info-key">Lig</span>
                <span class="info-value">{{ match.league || '-' }}</span>
              </div>
              <div class="info-item">
                <span class="info-key">Başlangıç</span>
                <span class="info-value">{{ formatDateTime(match.startTime) }}</span>
              </div>
              <div class="info-item">
                <span class="info-key">Durum</span>
                <span class="info-value">
                  <span v-if="match.status === 0">Başlamadı</span>
                  <span v-else-if="match.status === 1" class="text-live">Canlı ({{ match.minute }}')</span>
                  <span v-else-if="match.status === 2" class="text-halftime">Devre Arası</span>
                  <span v-else-if="match.status === 3">Tamamlandı</span>
                </span>
              </div>
              <div class="info-item">
                <span class="info-key">Spor</span>
                <span class="info-value">{{ getSportLabel(match.sportType) }}</span>
              </div>
              <div v-if="match.sportType === 0" class="info-item">
                <span class="info-key">Stadyum</span>
                <span class="info-value">{{ displayStadiumName }}</span>
              </div>
              <div class="info-item">
                <span class="info-key">Ev Sahibi</span>
                <span class="info-value">{{ match.homeTeam }}</span>
              </div>
              <div class="info-item">
                <span class="info-key">Deplasman</span>
                <span class="info-value">{{ match.awayTeam }}</span>
              </div>
            </div>
          </div>

          <!-- SOHBET SEKMESİ -->
          <div v-if="activeTab === 'chat'" class="tab-content tab-content--chat">
            <MatchChatPanel
              :match-id="match.id"
              :is-authenticated="authStore.isAuthenticated"
              :is-admin="authStore.isAdmin"
              :current-user-id="authStore.user?.id ?? null"
              :current-user-name="authStore.user?.userName ?? 'Ben'"
              :token="authStore.token"
            />
          </div>
        </div>
      </div>

    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { fetchMatchById } from '../api/matchApi'
import { useWebSocket } from '../composables/useWebSocket'
import { useFormatters } from '../composables/useFormatters'
import { SPORT_LABELS } from '../constants/sports'
import { useAuthStore } from '../stores/auth'
import MatchChatPanel from '../components/match/MatchChatPanel.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const goHome = () => router.push(authStore.isAuthenticated ? '/user' : '/')
const goToTeam = (teamId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/team/${teamId}`)
}
const { formatDateTime } = useFormatters()
const { connect } = useWebSocket()

/* =============================================
   DURUM DEĞİŞKENLERİ
   ============================================= */
const match = ref(null)
const isLoading = ref(true)
const isStatsLoading = ref(false)
const errorMessage = ref('')
const activeTab = ref('stats')

/* =============================================
   HESAPLANAN ÖZELLİKLER
   ============================================= */

// İstatistik verisi var mı kontrolü
const hasStatistics = computed(() => {
  const s = match.value?.statistics
  return s && Object.keys(s).length > 0
})

// Kadrolar (API camelCase: lineups.home.startingXI)
const matchLineups = computed(() => match.value?.lineups ?? null)

const homeStartingXI = computed(() => {
  const xi = matchLineups.value?.home?.startingXI
  return Array.isArray(xi) ? xi : []
})

const awayStartingXI = computed(() => {
  const xi = matchLineups.value?.away?.startingXI
  return Array.isArray(xi) ? xi : []
})

const homeLineupFormation = computed(() => matchLineups.value?.home?.formation || '')
const awayLineupFormation = computed(() => matchLineups.value?.away?.formation || '')

const hasLineups = computed(() => homeStartingXI.value.length > 0 || awayStartingXI.value.length > 0)

const formatLineupPos = (pos) => {
  if (!pos) return '–'
  const p = String(pos).toUpperCase()
  const map = { G: 'KL', D: 'DF', M: 'OS', F: 'SV' }
  return map[p] || p
}

const lineupPosTitle = (pos) => {
  if (!pos) return ''
  const p = String(pos).toUpperCase()
  const map = {
    G: 'Kaleci',
    D: 'Defans',
    M: 'Orta saha',
    F: 'Forvet'
  }
  return map[p] || pos
}

/** API camelCase + olası PascalCase; boşsa tire */
const displayStadiumName = computed(() => {
  const m = match.value
  if (!m) return '—'
  const raw = m.stadiumName ?? m.StadiumName
  if (raw != null && String(raw).trim() !== '') return String(raw).trim()
  return '—'
})

// Basketbol veya voleybol mu? (periyot tablosu gösterilecek)
const isPeriodicSport = computed(() => {
  const s = match.value?.statistics
  if (!s) return false
  return s.sportType === 'basketball' || s.sportType === 'volleyball'
})

// Periyot başlıkları (Basketbol: Q1-Q4+OT, Voleybol: Set1-Set5)
const periodHeaders = computed(() => {
  const s = match.value?.statistics
  if (!s) return []

  if (s.sportType === 'basketball') {
    const headers = [
      { key: 'Q1', label: 'Q1' },
      { key: 'Q2', label: 'Q2' },
      { key: 'Q3', label: 'Q3' },
      { key: 'Q4', label: 'Q4' }
    ]
    // Uzatma varsa ekle
    if (s.homeOT !== undefined || s.awayOT !== undefined) {
      headers.push({ key: 'OT', label: 'OT' })
    }
    return headers
  }

  if (s.sportType === 'volleyball') {
    const headers = []
    for (let i = 1; i <= 5; i++) {
      if (s[`homeSet${i}`] !== undefined || s[`awaySet${i}`] !== undefined) {
        headers.push({ key: `Set${i}`, label: `S${i}` })
      }
    }
    return headers
  }

  return []
})

// Maç olayları (goller, kartlar, değişiklikler) - timeline
const matchEvents = computed(() => {
  const events = match.value?.events
  if (!events || !Array.isArray(events)) return []

  return events.map(ev => {
    const type = (ev.type || '').toLowerCase()
    const detail = (ev.detail || '').toLowerCase()

    let icon = '📋'
    let iconClass = ''
    let label = ev.detail || ev.type || ''

    if (type === 'goal') {
      icon = '⚽'
      iconClass = 'icon-goal'
      if (detail.includes('penalty')) label = 'Penaltı Golü'
      else if (detail.includes('own')) label = 'Kendi Kalesine Gol'
      else label = 'Gol'
    } else if (type === 'card') {
      if (detail.includes('yellow') && !detail.includes('red')) {
        icon = '🟨'
        iconClass = 'icon-yellow'
        label = 'Sarı Kart'
      } else if (detail.includes('red') || detail.includes('second')) {
        icon = '🟥'
        iconClass = 'icon-red'
        label = detail.includes('second') ? 'İkinci Sarı (Kırmızı)' : 'Kırmızı Kart'
      }
    } else if (type === 'subst') {
      icon = '🔄'
      iconClass = 'icon-subst'
      label = 'Oyuncu Değişikliği'
    } else if (type === 'var') {
      icon = '📺'
      iconClass = 'icon-var'
      label = `VAR: ${ev.detail || ''}`
    }

    const isHome = ev.teamName === match.value?.homeTeam

    return {
      minute: ev.minute || 0,
      extraMinute: ev.extraMinute,
      icon,
      iconClass,
      label,
      type: type,
      playerName: ev.playerName || '',
      assistName: ev.assistName || '',
      teamName: ev.teamName || '',
      teamLogo: ev.teamLogo || '',
      isHome
    }
  }).sort((a, b) => a.minute - b.minute || (a.extraMinute || 0) - (b.extraMinute || 0))
})

// Futbol istatistikleri (bar grafik)
const matchStats = computed(() => {
  const s = match.value?.statistics
  if (!s) return []

  // Basketbol/voleybol için bar istatistik gösterme (periyot tablosu var)
  if (s.sportType === 'basketball' || s.sportType === 'volleyball') return []

  const stats = []

  if (s.homePossession || s.awayPossession) {
    stats.push({ label: 'Topa Sahiplik', home: s.homePossession || '0%', away: s.awayPossession || '0%' })
  }
  if (s.homeShotsTotal !== undefined || s.awayShotsTotal !== undefined) {
    stats.push({ label: 'Toplam Şut', home: s.homeShotsTotal ?? 0, away: s.awayShotsTotal ?? 0 })
  }
  if (s.homeShotsOnGoal !== undefined || s.awayShotsOnGoal !== undefined) {
    stats.push({ label: 'İsabetli Şut', home: s.homeShotsOnGoal ?? 0, away: s.awayShotsOnGoal ?? 0 })
  }
  if (s.homeBlockedShots !== undefined || s.awayBlockedShots !== undefined) {
    stats.push({ label: 'Engellenen Şut', home: s.homeBlockedShots ?? 0, away: s.awayBlockedShots ?? 0 })
  }
  if (s.homeCorners !== undefined || s.awayCorners !== undefined) {
    stats.push({ label: 'Korner', home: s.homeCorners ?? 0, away: s.awayCorners ?? 0 })
  }
  if (s.homeOffsides !== undefined || s.awayOffsides !== undefined) {
    stats.push({ label: 'Ofsayt', home: s.homeOffsides ?? 0, away: s.awayOffsides ?? 0 })
  }
  if (s.homeFouls !== undefined || s.awayFouls !== undefined) {
    stats.push({ label: 'Faul', home: s.homeFouls ?? 0, away: s.awayFouls ?? 0 })
  }
  if (s.homeYellowCards !== undefined || s.awayYellowCards !== undefined) {
    stats.push({ label: 'Sarı Kart', home: s.homeYellowCards ?? 0, away: s.awayYellowCards ?? 0 })
  }
  if (s.homeRedCards !== undefined || s.awayRedCards !== undefined) {
    stats.push({ label: 'Kırmızı Kart', home: s.homeRedCards ?? 0, away: s.awayRedCards ?? 0 })
  }
  if (s.homeSaves !== undefined || s.awaySaves !== undefined) {
    stats.push({ label: 'Kaleci Kurtarışı', home: s.homeSaves ?? 0, away: s.awaySaves ?? 0 })
  }
  if (s.homeTotalPasses !== undefined || s.awayTotalPasses !== undefined) {
    stats.push({ label: 'Toplam Pas', home: s.homeTotalPasses ?? 0, away: s.awayTotalPasses ?? 0 })
  }
  if (s.homePassAccuracy || s.awayPassAccuracy) {
    stats.push({ label: 'Pas İsabeti', home: s.homePassAccuracy || '0%', away: s.awayPassAccuracy || '0%' })
  }

  return stats
})

/* =============================================
   YARDIMCI FONKSİYONLAR
   ============================================= */
const getTeamInitial = (teamName) => {
  if (!teamName) return '?'
  return teamName.charAt(0).toUpperCase()
}

const getSportLabel = (sportType) => {
  return SPORT_LABELS[sportType] || 'Bilinmiyor'
}

const getBarPercent = (val, other) => {
  const v = parseFloat(val) || 0
  const o = parseFloat(other) || 0
  if (v + o === 0) return 50
  return Math.round((v / (v + o)) * 100)
}

/* =============================================
   WEBSOCKET (Composable ile)
   ============================================= */
const setupLiveUpdates = () => {
  connect({
    MatchUpdated: (updatedMatch) => {
      if (match.value && updatedMatch.id === match.value.id) {
        match.value.homeScore = updatedMatch.homeScore
        match.value.awayScore = updatedMatch.awayScore
        match.value.minute = updatedMatch.minute
        match.value.status = updatedMatch.status
        if (updatedMatch.homeTeamLogo) match.value.homeTeamLogo = updatedMatch.homeTeamLogo
        if (updatedMatch.awayTeamLogo) match.value.awayTeamLogo = updatedMatch.awayTeamLogo
        console.log(`Canlı güncelleme: ${updatedMatch.homeTeam} ${updatedMatch.homeScore}-${updatedMatch.awayScore} ${updatedMatch.awayTeam}`)
      }
    },
    AllMatches: (allMatches) => {
      if (!match.value || !Array.isArray(allMatches)) return
      const updated = allMatches.find(m => m.id === match.value.id)
      if (updated) {
        match.value.homeScore = updated.homeScore
        match.value.awayScore = updated.awayScore
        match.value.minute = updated.minute
        match.value.status = updated.status
        if (updated.homeTeamLogo) match.value.homeTeamLogo = updated.homeTeamLogo
        if (updated.awayTeamLogo) match.value.awayTeamLogo = updated.awayTeamLogo
      }
    }
  })
}

/* =============================================
   YAŞAM DÖNGÜSÜ
   ============================================= */
onMounted(async () => {
  const matchId = route.params.id

  setupLiveUpdates()

  try {
    isStatsLoading.value = true
    match.value = await fetchMatchById(matchId)
    console.log('Maç verisi:', match.value)

    if (match.value?.statistics) {
      console.log('İstatistikler yüklendi:', Object.keys(match.value.statistics).length, 'alan')
    }
  } catch (error) {
    console.error('Maç yüklenemedi:', error)
    errorMessage.value = 'Maç yüklenemedi.'
  } finally {
    isLoading.value = false
    isStatsLoading.value = false
  }
})
</script>

<style scoped>
/* =============================================
   YÜKLENİYOR & HATA DURUMU
   ============================================= */
.state-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  padding: 4rem 2rem;
  min-height: 50vh;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid #21262d;
  border-top-color: #58a6ff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

.state-icon { font-size: 2.5rem; opacity: 0.5; }
.state-text { font-size: 0.95rem; color: #8b949e; }

.btn-go-home {
  background: #21262d;
  color: #c9d1d9;
  border: 1px solid #30363d;
  padding: 0.5rem 1.25rem;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  margin-top: 0.5rem;
}

.btn-go-home:hover { background: #30363d; }

/* =============================================
   SKOR BÖLÜMÜ
   ============================================= */
.score-section {
  background: linear-gradient(180deg, #161b22 0%, #0d1117 100%);
  border-bottom: 1px solid #21262d;
  padding: 2rem 1.5rem 2.5rem;
}

.league-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  margin-bottom: 1.75rem;
}

.league-badge-icon { font-size: 0.85rem; }

.league-badge-text {
  font-size: 0.8rem;
  font-weight: 600;
  color: #8b949e;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.scoreboard {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2rem;
  max-width: 600px;
  margin: 0 auto;
}

.team-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  min-width: 0;
}
.team-block.team-clickable {
  cursor: pointer;
  border-radius: 8px;
  padding: 0.4rem;
  margin: -0.4rem;
  transition: background 0.15s;
}
.team-block.team-clickable:hover { background: #21262d; }

.team-emblem {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: #21262d;
  border: 2px solid #30363d;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: border-color 0.3s;
  overflow: hidden;
}

.team-emblem.has-logo {
  background: #1c2129;
  border-color: #30363d;
}

.emblem-img {
  width: 52px;
  height: 52px;
  object-fit: contain;
}

.emblem-letter { font-size: 1.8rem; font-weight: 700; color: #c9d1d9; }

.team-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: #e1e4e8;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 150px;
}

.score-center {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.6rem;
  flex-shrink: 0;
}

.score-digits { display: flex; align-items: center; gap: 0.5rem; }

.digit {
  font-size: 3rem;
  font-weight: 800;
  color: #e1e4e8;
  line-height: 1;
  min-width: 45px;
  text-align: center;
}

.digit.winner { color: #ffffff; }
.score-separator { font-size: 2rem; font-weight: 300; color: #484f58; }

/* Maç durumu etiketleri */
.match-status {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.78rem;
  font-weight: 600;
  padding: 0.3rem 0.85rem;
  border-radius: 12px;
}

.match-status.live {
  background: #f8514922;
  color: #f85149;
  animation: glow 2s ease-in-out infinite;
}

@keyframes glow {
  0%, 100% { box-shadow: 0 0 0 0 #f8514900; }
  50% { box-shadow: 0 0 12px 2px #f8514933; }
}

.live-pulse {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #f85149;
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.3; }
}

.match-status.halftime { background: #d2992222; color: #d29922; }
.match-status.finished { background: #21262d; color: #8b949e; }
.match-status.upcoming { background: #58a6ff15; color: #58a6ff; }

/* =============================================
   SEKMELİ İÇERİK ALANI
   ============================================= */
.content-area {
  max-width: 700px;
  margin: 0 auto;
  padding: 1.5rem;
}

.content-card {
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 12px;
  overflow: hidden;
}

.tab-bar {
  display: flex;
  border-bottom: 1px solid #21262d;
  background: #0d1117;
}

.tab-item {
  flex: 1;
  padding: 0.75rem;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: #8b949e;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.tab-item:hover { color: #c9d1d9; background: #161b22; }
.tab-item.active { color: #58a6ff; border-bottom-color: #58a6ff; }

.tab-content { padding: 1rem 1.25rem; }

/* =============================================
   İSTATİSTİK YÜKLENİYOR / BOŞ
   ============================================= */
.stats-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 2.5rem;
  color: #8b949e;
  font-size: 0.85rem;
}

.spinner-sm {
  width: 24px;
  height: 24px;
  border: 2px solid #21262d;
  border-top-color: #58a6ff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.stats-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2.5rem;
  text-align: center;
}

.stats-empty-icon { font-size: 2rem; opacity: 0.35; }
.stats-empty p { font-size: 0.9rem; font-weight: 500; color: #8b949e; margin: 0; }
.stats-empty small { font-size: 0.78rem; color: #484f58; max-width: 280px; line-height: 1.5; }

/* =============================================
   PERİYOT TABLOSU (Basketbol / Voleybol)
   ============================================= */
.period-stats {
  overflow-x: auto;
}

.period-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;
}

.period-table thead {
  background: #0d1117;
}

.period-table th {
  padding: 0.65rem 0.5rem;
  font-weight: 600;
  color: #8b949e;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  border-bottom: 1px solid #21262d;
}

.period-table td {
  padding: 0.7rem 0.5rem;
  border-bottom: 1px solid #21262d15;
  color: #c9d1d9;
  font-weight: 500;
}

.period-table tbody tr:hover {
  background: #1c2129;
}

.period-team-col {
  text-align: left !important;
  min-width: 120px;
}

.period-team-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.period-team-logo {
  width: 20px;
  height: 20px;
  object-fit: contain;
  border-radius: 2px;
}

.period-col {
  text-align: center !important;
  min-width: 40px;
  font-variant-numeric: tabular-nums;
}

.period-total {
  font-weight: 700 !important;
  color: #ffffff !important;
  background: #161b2280;
}

/* =============================================
   İSTATİSTİKLER (Futbol bar grafiği)
   ============================================= */
.stat-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.7rem 0;
  border-bottom: 1px solid #21262d15;
}

.stat-row:last-child { border-bottom: none; }

.stat-home-val,
.stat-away-val {
  font-size: 0.85rem;
  font-weight: 700;
  color: #e1e4e8;
  min-width: 36px;
  text-align: center;
}

.stat-center {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
}

.stat-label {
  font-size: 0.75rem;
  font-weight: 500;
  color: #8b949e;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.stat-bar-wrapper { display: flex; gap: 4px; width: 100%; }

.stat-bar {
  flex: 1;
  height: 4px;
  background: #21262d;
  border-radius: 2px;
  overflow: hidden;
}

.stat-bar:first-child { direction: rtl; }

.stat-bar-fill {
  height: 100%;
  border-radius: 2px;
  transition: width 0.5s ease;
}

.home-fill { background: #58a6ff; }
.away-fill { background: #f0883e; }

/* =============================================
   OLAYLAR TIMELINE
   ============================================= */
.events-timeline {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.event-item {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.6rem 0.5rem;
  border-radius: 6px;
  transition: background 0.2s;
}

.event-item:hover {
  background: #1c2129;
}

.event-item.event-home {
  flex-direction: row;
}

.event-item.event-away {
  flex-direction: row-reverse;
  text-align: right;
}

.event-item.event-away .event-detail {
  align-items: flex-end;
}

.event-minute {
  min-width: 36px;
  font-size: 0.78rem;
  font-weight: 700;
  color: #8b949e;
  text-align: center;
  font-variant-numeric: tabular-nums;
}

.event-extra {
  font-size: 0.65rem;
  color: #f85149;
}

.event-icon {
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.9rem;
  border-radius: 50%;
  background: #21262d;
  flex-shrink: 0;
}

.icon-goal { background: #238636; }
.icon-yellow { background: #d29922; }
.icon-red { background: #f85149; }
.icon-subst { background: #1f6feb; }
.icon-var { background: #8957e5; }

.event-detail {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  min-width: 0;
}

.event-player {
  font-size: 0.82rem;
  font-weight: 600;
  color: #e1e4e8;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.event-assist {
  font-size: 0.72rem;
  color: #8b949e;
  font-weight: 400;
}

.event-type-label {
  font-size: 0.68rem;
  color: #484f58;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.event-team {
  flex-shrink: 0;
}

.event-team-logo {
  width: 20px;
  height: 20px;
  object-fit: contain;
}

/* =============================================
   KADROLAR (İlk 11)
   ============================================= */
.lineups-wrap {
  padding: 0.25rem 0;
}

.lineups-columns {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.lineup-side {
  min-width: 0;
  border: 1px solid #21262d;
  border-radius: 10px;
  background: #0d1117;
  overflow: hidden;
}

.lineup-side-home {
  border-top: 3px solid #58a6ff;
}

.lineup-side-away {
  border-top: 3px solid #f0883e;
}

.lineup-side-head {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.75rem 0.85rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.lineup-side-logo {
  width: 28px;
  height: 28px;
  object-fit: contain;
  border-radius: 4px;
  flex-shrink: 0;
}

.lineup-side-titles {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
}

.lineup-side-name {
  font-size: 0.8rem;
  font-weight: 700;
  color: #e1e4e8;
  line-height: 1.25;
  word-break: break-word;
}

.lineup-formation {
  font-size: 0.68rem;
  font-weight: 600;
  color: #8b949e;
  letter-spacing: 0.4px;
}

.lineup-list {
  list-style: none;
  margin: 0;
  padding: 0.35rem 0;
  counter-reset: lineup;
}

.lineup-row {
  display: grid;
  grid-template-columns: 28px 28px 1fr;
  align-items: center;
  gap: 0.4rem;
  padding: 0.45rem 0.85rem;
  font-size: 0.8rem;
  border-bottom: 1px solid #21262d12;
}

.lineup-row:last-child {
  border-bottom: none;
}

.lineup-num {
  font-weight: 800;
  color: #58a6ff;
  font-variant-numeric: tabular-nums;
  text-align: center;
  font-size: 0.78rem;
}

.lineup-side-away .lineup-num {
  color: #f0883e;
}

.lineup-pos {
  font-size: 0.65rem;
  font-weight: 700;
  color: #484f58;
  text-align: center;
  text-transform: uppercase;
}

.lineup-player {
  color: #c9d1d9;
  font-weight: 500;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 560px) {
  .lineups-columns {
    grid-template-columns: 1fr;
  }
}

/* =============================================
   BİLGİLER SEKMESİ
   ============================================= */
.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 0.85rem 0.5rem;
  border-bottom: 1px solid #21262d15;
}

.info-item:nth-child(odd) { border-right: 1px solid #21262d15; padding-right: 1rem; }
.info-item:nth-child(even) { padding-left: 1rem; }

.info-key {
  font-size: 0.7rem;
  font-weight: 500;
  color: #484f58;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-value { font-size: 0.85rem; font-weight: 600; color: #e1e4e8; }
.text-live { color: #f85149; }
.text-halftime { color: #d29922; }

/* =============================================
   MOBİL UYUMLULUK
   ============================================= */
@media (max-width: 700px) {
  .scoreboard { gap: 1rem; }
  .digit { font-size: 2.2rem; min-width: 32px; }
  .team-emblem { width: 56px; height: 56px; }
  .emblem-img { width: 40px; height: 40px; }
  .emblem-letter { font-size: 1.4rem; }
  .team-label { font-size: 0.8rem; max-width: 100px; }
  .content-area { padding: 1rem; }

  .info-grid { grid-template-columns: 1fr; }
  .info-item:nth-child(odd) { border-right: none; padding-right: 0.5rem; }
  .info-item:nth-child(even) { padding-left: 0.5rem; }
}

@media (max-width: 500px) {
  .score-section { padding: 1.5rem 1rem 2rem; }
  .scoreboard { gap: 0.75rem; }
  .digit { font-size: 1.8rem; }
  .team-emblem { width: 48px; height: 48px; }
  .emblem-img { width: 34px; height: 34px; }
  .emblem-letter { font-size: 1.2rem; }
}

/* =============================================
   SOHBET SEKMESİ
   ============================================= */
.tab-content--chat {
  padding: 0;
}
</style>
