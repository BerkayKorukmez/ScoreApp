<template>
  <div class="past-matches-page">

    <!-- TAB SEÇİCİ -->
    <div class="tab-bar">
      <button
        :class="['tab-btn', { active: activeTab === 'date' }]"
        @click="activeTab = 'date'"
      >
        📅 Tarihe Göre
      </button>
      <button
        :class="['tab-btn', { active: activeTab === 'league' }]"
        @click="activeTab = 'league'"
      >
        🏆 Lig Sonuçları
      </button>
    </div>

    <!-- ================================================================
         TAB 1: TARİHE GÖRE (mevcut)
         ================================================================ -->
    <div v-if="activeTab === 'date'">

    <!-- SAYFA BAŞLIĞI VE FİLTRELER -->
    <div class="page-header">
      <div class="header-title-section">
        <h1 class="page-title">📅 Geçmiş Maçlar</h1>
        <p class="page-desc">
          Tarih seçin — tüm futbol liglerinden geçmiş maç sonuçlarını görüntüleyin
        </p>
      </div>

      <div class="header-filters">
        <!-- Tarih Seçimi -->
        <div class="filter-group">
          <label class="filter-label">Tarih</label>
          <div class="date-picker-wrapper">
            <button class="date-nav-btn double" @click="changeDate(-7)" title="7 gün geri">««</button>
            <button class="date-nav-btn" @click="changeDate(-1)" title="1 gün geri">‹</button>
            <input
              v-model="selectedDate"
              type="date"
              class="date-input"
              :max="maxDate"
              @change="loadHistory"
            />
            <button
              class="date-nav-btn"
              @click="changeDate(1)"
              :disabled="selectedDate >= maxDate"
              title="1 gün ileri"
            >›</button>
            <button
              class="date-nav-btn double"
              @click="changeDate(7)"
              :disabled="selectedDate >= maxDate"
              title="7 gün ileri"
            >»»</button>
          </div>
        </div>

        <!-- Lig Seçimi (optgroup bazı tarayıcılarda/Vue ile boş kalıyor — düz liste + ayırıcı) -->
        <div class="filter-group league-filter-group">
          <label class="filter-label">Lig</label>
          <select v-model="selectedLeagueKey" class="filter-select league-select">
            <option value="">Tüm Ligler</option>
            <optgroup
              v-for="group in leaguesByCountry"
              :key="group.country"
              :label="group.country"
            >
              <option
                v-for="league in group.leagues"
                :key="league.key"
                :value="league.key"
              >
                {{ league.displayName || league.name }}
              </option>
            </optgroup>
          </select>
        </div>

        <!-- Ara Butonu -->
        <div class="filter-group">
          <label class="filter-label">&nbsp;</label>
          <button class="search-btn" @click="loadHistory" :disabled="isLoading">
            <template v-if="isLoading">
              <div class="spinner-xs"></div>
            </template>
            <template v-else>🔍 Ara</template>
          </button>
        </div>
      </div>

      <!-- Hızlı Tarih Kısayolları -->
      <div class="quick-dates">
        <button
          v-for="shortcut in dateShortcuts"
          :key="shortcut.label"
          :class="['quick-date-btn', { active: selectedDate === shortcut.date }]"
          @click="selectQuickDate(shortcut.date)"
        >
          {{ shortcut.label }}
        </button>
      </div>
    </div>

    <!-- Tarih Bilgi Çubuğu -->
    <div class="date-info-bar" v-if="lastSearchedDate">
      <span class="date-info-text">
        📆 {{ formatDateTurkish(lastSearchedDate) }}
        <span v-if="selectedLeagueDisplay"> · {{ selectedLeagueDisplay }}</span>
      </span>
      <span class="match-count-badge" v-if="!isLoading">
        {{ displayedMatches.length }} maç
      </span>
    </div>

    <!-- YÜKLENİYOR -->
    <div v-if="isLoading" class="loading-state">
      <div class="spinner"></div>
      <span>Maçlar yükleniyor...</span>
    </div>

    <!-- HATA DURUMU -->
    <div v-else-if="errorMessage" class="error-state">
      <span class="error-icon">⚠️</span>
      <p>{{ errorMessage }}</p>
      <button class="retry-btn" @click="loadHistory">Tekrar Dene</button>
    </div>

    <!-- HENüz ARAMA YAPILMADI -->
    <div v-else-if="!lastSearchedDate" class="empty-state initial-state">
      <span class="empty-icon">🏟️</span>
      <p>Tarih seçin ve arayın</p>
      <small>Bir tarih seçip "Ara" butonuna tıklayarak geçmiş maçları görüntüleyebilirsiniz</small>
    </div>

    <!-- MAÇ BULUNAMADI -->
    <div v-else-if="displayedMatches.length === 0" class="empty-state">
      <span class="empty-icon">📭</span>
      <p>Maç bulunamadı</p>
      <small>Bu tarihte{{ selectedLeagueDisplay ? ` (${selectedLeagueDisplay})` : '' }} maç sonucu bulunamadı</small>
    </div>

    <!-- MAÇ LİSTESİ (Liglere göre gruplanmış) -->
    <div v-else class="matches-container">
      <div
        v-for="(group, leagueKey) in groupedMatches"
        :key="leagueKey"
        class="league-group"
      >
        <!-- Lig Başlığı -->
        <div class="league-header">
          <div class="league-title">
            <img
              v-if="getLeagueFlag(group[0])"
              :src="getLeagueFlag(group[0])"
              class="league-flag"
              @error="$event.target.style.display='none'"
            />
            <span v-else class="league-icon">🏆</span>
            <span class="league-country-label" v-if="group[0]?.leagueCountry">
              {{ group[0].leagueCountry }} ·
            </span>
            <span class="league-name">{{ getLeagueName(leagueKey) }}</span>
          </div>
          <span class="league-match-count">{{ group.length }} maç</span>
        </div>

        <!-- O ligteki maçlar -->
        <div
          v-for="match in group"
          :key="match.id"
          class="match-row"
          @click="goToMatchDetail(match.id)"
        >
          <!-- Maç saati / durumu -->
          <div class="match-time-col">
            <span class="match-clock">{{ formatTime(match.startTime) }}</span>
            <span
              class="status-badge"
              :class="getStatusClass(match.status)"
            >
              {{ getStatusText(match.status) }}
            </span>
          </div>

          <!-- Takımlar ve skor -->
          <div class="match-teams-col">
            <div class="team-row">
              <div
                class="team-info"
                :class="{ 'team-clickable': !!match.homeTeamId }"
                @click.stop="match.homeTeamId && goToTeam(match.homeTeamId)"
              >
                <img
                  v-if="match.homeTeamLogo"
                  :src="match.homeTeamLogo"
                  :alt="match.homeTeam"
                  class="team-logo-sm"
                  loading="lazy"
                  @error="$event.target.style.display='none'"
                />
                <span class="team-logo-placeholder" v-else>{{ match.homeTeam?.charAt(0) }}</span>
                <span class="team-name" :class="{ winner: match.status === 3 && match.homeScore > match.awayScore }">
                  {{ match.homeTeam }}
                </span>
              </div>
              <span class="team-score">{{ match.homeScore }}</span>
            </div>
            <div class="team-row">
              <div
                class="team-info"
                :class="{ 'team-clickable': !!match.awayTeamId }"
                @click.stop="match.awayTeamId && goToTeam(match.awayTeamId)"
              >
                <img
                  v-if="match.awayTeamLogo"
                  :src="match.awayTeamLogo"
                  :alt="match.awayTeam"
                  class="team-logo-sm"
                  loading="lazy"
                  @error="$event.target.style.display='none'"
                />
                <span class="team-logo-placeholder" v-else>{{ match.awayTeam?.charAt(0) }}</span>
                <span class="team-name" :class="{ winner: match.status === 3 && match.awayScore > match.homeScore }">
                  {{ match.awayTeam }}
                </span>
              </div>
              <span class="team-score">{{ match.awayScore }}</span>
            </div>
          </div>

          <!-- Detay butonu -->
          <div class="match-detail-col">
            <span class="detail-arrow">→</span>
          </div>
        </div>
      </div>
    </div>

    </div> <!-- /activeTab === 'date' -->

    <!-- ================================================================
         TAB 2: LİG SONUÇLARI (CollectAPI)
         ================================================================ -->
    <div v-if="activeTab === 'league'" class="league-results-tab">

      <!-- Lig seçici -->
      <div class="lr-filters">
        <div class="filter-group">
          <label class="filter-label">Lig Seç</label>
          <select v-model="selectedResultLeague" class="filter-select league-select" @change="loadLeagueResults">
            <option v-for="l in footballLeagues" :key="l.key" :value="l">
              {{ l.displayName || l.name }}
            </option>
          </select>
        </div>
        <div class="filter-group">
          <label class="filter-label">&nbsp;</label>
          <button class="search-btn" @click="loadLeagueResults" :disabled="isResultsLoading">
            <div v-if="isResultsLoading" class="spinner-xs"></div>
            <template v-else>🔄 Yenile</template>
          </button>
        </div>
      </div>

      <!-- Yükleniyor -->
      <div v-if="isResultsLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Sonuçlar yükleniyor...</span>
      </div>

      <!-- Boş -->
      <div v-else-if="leagueResults.length === 0" class="empty-state">
        <span class="empty-icon">📭</span>
        <p>Sonuç bulunamadı</p>
        <small>Bu lig için son hafta verisi mevcut değil</small>
      </div>

      <!-- Sonuçlar -->
      <div v-else class="matches-container">
        <div class="league-group">
          <div class="league-header">
            <div class="league-title">
              <img
                v-if="selectedResultLeague?.flag"
                :src="selectedResultLeague.flag"
                class="league-flag"
                @error="$event.target.style.display='none'"
              />
              <span v-else class="league-icon">🏆</span>
              <span class="league-name">{{ selectedResultLeague?.displayName || selectedResultLeague?.name }}</span>
            </div>
            <span class="league-match-count">{{ leagueResults.length }} maç</span>
          </div>

          <div
            v-for="(match, idx) in leagueResults"
            :key="idx"
            class="match-row result-row"
          >
            <!-- Tarih -->
            <div class="match-time-col">
              <span class="match-clock">{{ formatResultDate(match.date) }}</span>
              <span class="status-badge" :class="match.isPlayed ? 'finished' : 'not-started'">
                {{ match.isPlayed ? 'BT' : 'BS' }}
              </span>
            </div>

            <!-- Takımlar ve skor -->
            <div class="match-teams-col">
              <div class="team-row">
                <div class="team-info">
                  <span class="team-logo-placeholder">{{ match.homeTeam?.charAt(0) }}</span>
                  <span class="team-name" :class="{ winner: match.isPlayed && match.homeScore > match.awayScore }">
                    {{ match.homeTeam }}
                  </span>
                </div>
                <span class="team-score">{{ match.isPlayed ? match.homeScore : '-' }}</span>
              </div>
              <div class="team-row">
                <div class="team-info">
                  <span class="team-logo-placeholder">{{ match.awayTeam?.charAt(0) }}</span>
                  <span class="team-name" :class="{ winner: match.isPlayed && match.awayScore > match.homeScore }">
                    {{ match.awayTeam }}
                  </span>
                </div>
                <span class="team-score">{{ match.isPlayed ? match.awayScore : '-' }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div> <!-- /activeTab === 'league' -->

  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { fetchMatchHistory, fetchFootballResults } from '../api/matchApi'
import { useFormatters } from '../composables/useFormatters'
import { SPORTS, POPULAR_LEAGUES, POPULAR_STANDINGS_LEAGUES } from '../constants/sports'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const { formatTime } = useFormatters()

/* ============ TAB ============ */
const activeTab = ref('date')

/* =============================================
   DURUM DEĞİŞKENLERİ
   ============================================= */
const selectedSport = ref('football')
const selectedDate = ref(getYesterday())
const selectedLeagueKey = ref('')
const isLoading = ref(false)
const errorMessage = ref('')
const allMatches = ref([])
const lastSearchedDate = ref(null)
const lastSearchedSport = ref(null)

// Bugünün tarihi (max olarak kullanılır) — yerel gün
const maxDate = computed(() => toYyyyMmDdLocal(new Date()))

// Hızlı tarih kısayolları
const dateShortcuts = computed(() => {
  const shortcuts = []
  const today = new Date()

  const addShortcut = (label, daysBack) => {
    const d = new Date(today)
    d.setDate(d.getDate() - daysBack)
    shortcuts.push({ label, date: toYyyyMmDdLocal(d) })
  }

  addShortcut('Bugün', 0)
  addShortcut('Dün', 1)
  addShortcut('2 Gün Önce', 2)
  addShortcut('3 Gün Önce', 3)
  addShortcut('1 Hafta Önce', 7)
  addShortcut('2 Hafta Önce', 14)
  addShortcut('1 Ay Önce', 30)

  return shortcuts
})

/* =============================================
   YARDIMCI FONKSİYONLAR
   ============================================= */
/** Yerel takvim günü yyyy-MM-dd (toISOString UTC kayması yapmaz — TR saati için kritik) */
function toYyyyMmDdLocal(d) {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function getYesterday() {
  const d = new Date()
  d.setDate(d.getDate() - 1)
  return toYyyyMmDdLocal(d)
}

function changeDate(days) {
  const parts = selectedDate.value.split('-').map(Number)
  const d = new Date(parts[0], parts[1] - 1, parts[2])
  d.setDate(d.getDate() + days)
  const newDate = toYyyyMmDdLocal(d)
  if (newDate <= maxDate.value) {
    selectedDate.value = newDate
    loadHistory()
  }
}

function selectQuickDate(date) {
  selectedDate.value = date
  loadHistory()
}

function formatDateTurkish(dateStr) {
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  })
}

const getMatchLeagueKey = (match) => {
  if (match.leagueCountry) {
    return `${match.leagueCountry}::${match.league}`
  }
  return `::${match.league}`
}

const getLeagueFlag = (match) => match?.leagueFlag || null

const getLeagueName = (leagueKey) => {
  const parts = leagueKey.split('::')
  return parts.length > 1 ? parts[1] : leagueKey
}

const goToMatchDetail = (matchId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/match/${matchId}`)
}

const goToTeam = (teamId) => {
  const prefix = authStore.isAuthenticated ? '/user' : ''
  router.push(`${prefix}/team/${teamId}`)
}

const getStatusClass = (status) => {
  switch (status) {
    case 3: return 'finished'
    case 1: return 'live'
    case 2: return 'halftime'
    default: return 'not-started'
  }
}

const getStatusText = (status) => {
  switch (status) {
    case 3: return 'BT'
    case 1: return 'CANLI'
    case 2: return 'IY'
    case 0: return 'BS'
    default: return '-'
  }
}

/* =============================================
   HESAPLANAN ÖZELLİKLER
   ============================================= */

const PRIORITY_COUNTRIES = ['Turkey', 'England', 'Spain', 'Italy', 'Germany', 'France', 'World']

/** Maçlardan + sabit popüler ligler (arama yapılmadan önce de dropdown dolu — Home/useLeagues ile aynı mantık) */
const leaguesByCountry = computed(() => {
  const sportData = SPORTS.find((s) => s.id === selectedSport.value)
  if (!sportData) return []

  const leagueMap = new Map()

  allMatches.value
    .filter((m) => Number(m.sportType) === Number(sportData.sportType))
    .forEach((m) => {
      if (!m.league) return
      const key = getMatchLeagueKey(m)
      if (!leagueMap.has(key)) {
        leagueMap.set(key, {
          key,
          name: m.league,
          displayName: m.league,
          country: m.leagueCountry || 'Diğer',
          leagueId: m.externalLeagueId ?? null,
          flag: m.leagueFlag ?? null,
          collectApiKey: null
        })
      }
    })

  const countryGroups = {}
  for (const league of leagueMap.values()) {
    if (!countryGroups[league.country]) countryGroups[league.country] = []
    countryGroups[league.country].push(league)
  }

  // Popüler ligleri merge et — sonuç gelmeden önce de "Tüm Ligler" dışında seçenekler görünsün
  const sportKey = sportData.id
  const popularList =
    POPULAR_STANDINGS_LEAGUES?.[sportKey] ??
    POPULAR_STANDINGS_LEAGUES?.[selectedSport.value] ??
    POPULAR_STANDINGS_LEAGUES?.football ??
    []

  popularList.forEach((pop) => {
    const key = `${pop.country}::${pop.name}`
    if (!leagueMap.has(key)) {
      leagueMap.set(key, {
        key,
        name: pop.name,
        displayName: pop.displayName ?? pop.name,
        country: pop.country,
        leagueId: pop.leagueId ?? null,
        flag: pop.flag ?? null,
        collectApiKey: pop.collectApiKey ?? null
      })
      if (!countryGroups[pop.country]) countryGroups[pop.country] = []
      countryGroups[pop.country].push(leagueMap.get(key))
    } else {
      const existing = leagueMap.get(key)
      if (!existing.displayName && pop.displayName) existing.displayName = pop.displayName
      if (!existing.collectApiKey && pop.collectApiKey) existing.collectApiKey = pop.collectApiKey
      if (!existing.leagueId && pop.leagueId) existing.leagueId = pop.leagueId
      if (!existing.flag && pop.flag) existing.flag = pop.flag
    }
  })

  for (const country of Object.keys(countryGroups)) {
    countryGroups[country].sort((a, b) => {
      const ia = POPULAR_LEAGUES.indexOf(a.name)
      const ib = POPULAR_LEAGUES.indexOf(b.name)
      if (ia !== -1 && ib !== -1) return ia - ib
      if (ia !== -1) return -1
      if (ib !== -1) return 1
      return a.name.localeCompare(b.name, 'tr-TR')
    })
  }

  const sortedCountries = Object.keys(countryGroups).sort((a, b) => {
    const ia = PRIORITY_COUNTRIES.indexOf(a)
    const ib = PRIORITY_COUNTRIES.indexOf(b)
    if (ia !== -1 && ib !== -1) return ia - ib
    if (ia !== -1) return -1
    if (ib !== -1) return 1
    return a.localeCompare(b, 'tr-TR')
  })

  return sortedCountries.map((country) => ({
    country,
    leagues: countryGroups[country] ?? []
  }))
})

/** Seçili lig adı (bilgi çubuğunda gösterilir) */
const selectedLeagueDisplay = computed(() => {
  if (!selectedLeagueKey.value) return null
  for (const group of leaguesByCountry.value) {
    const found = group.leagues.find(l => l.key === selectedLeagueKey.value)
    if (found) {
      return found.country !== 'Diğer'
        ? `${found.country} - ${found.name}`
        : found.name
    }
  }
  return null
})

/** Filtrelenmiş maçlar (lig seçimine göre) */
const displayedMatches = computed(() => {
  let filtered = allMatches.value

  if (selectedLeagueKey.value) {
    filtered = filtered.filter(m => getMatchLeagueKey(m) === selectedLeagueKey.value)
  }

  return filtered.sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
})

/** Liglere göre gruplandırılmış maçlar */
const groupedMatches = computed(() => {
  const groups = {}
  displayedMatches.value.forEach(match => {
    const key = getMatchLeagueKey(match)
    if (!groups[key]) groups[key] = []
    groups[key].push(match)
  })
  return groups
})

/* =============================================
   VERİ YÜKLEME
   ============================================= */
const loadHistory = async () => {
  if (!selectedDate.value) return

  isLoading.value = true
  errorMessage.value = ''

  try {
    const data = await fetchMatchHistory(selectedSport.value, selectedDate.value)
    allMatches.value = Array.isArray(data) ? data : []
    lastSearchedDate.value = selectedDate.value
    lastSearchedSport.value = selectedSport.value
    selectedLeagueKey.value = ''
    console.log(`Geçmiş maçlar yüklendi: ${allMatches.value.length} adet (${selectedSport.value}, ${selectedDate.value})`)
  } catch (error) {
    console.error('Geçmiş maçlar yüklenemedi:', error)
    if (error.response?.status === 429) {
      errorMessage.value = 'API istek limiti aşıldı. Lütfen biraz bekleyip tekrar deneyin.'
    } else {
      errorMessage.value = 'Maçlar yüklenirken bir hata oluştu. Lütfen tekrar deneyin.'
    }
    allMatches.value = []
  } finally {
    isLoading.value = false
  }
}

/* =============================================
   WATCHERS
   ============================================= */
// Spor değiştiğinde seçili ligi sıfırla ve otomatik ara
// Sayfa açılışında dünün maçlarını otomatik yükle
onMounted(() => {
  if (activeTab.value === 'date') {
    loadHistory()
  }
})

/* =============================================
   LİG SONUÇLARI (API-Sports)
   ============================================= */
const footballLeagues = computed(() =>
  (POPULAR_STANDINGS_LEAGUES.football ?? []).filter(l => !!l.leagueId)
)

const selectedResultLeague = ref(null)
const leagueResults        = ref([])
const isResultsLoading     = ref(false)

// Tab açılınca ilk ligi otomatik seç
watch(activeTab, (tab) => {
  if (tab === 'league' && !selectedResultLeague.value && footballLeagues.value.length) {
    selectedResultLeague.value = footballLeagues.value[0]
    loadLeagueResults()
  }
})

const loadLeagueResults = async () => {
  const league = selectedResultLeague.value
  if (!league?.leagueId) return

  isResultsLoading.value = true
  leagueResults.value    = []
  try {
    leagueResults.value = await fetchFootballResults(league.leagueId)
  } catch (err) {
    console.error('Lig sonuçları yüklenemedi:', err)
  } finally {
    isResultsLoading.value = false
  }
}

const formatResultDate = (dateStr) => {
  if (!dateStr) return '-'
  const d = new Date(dateStr)
  if (isNaN(d)) return dateStr
  return d.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' })
}
</script>

<style scoped>
/* =============================================
   SAYFA GENEL
   ============================================= */
.past-matches-page {
  max-width: 1000px;
  margin: 0 auto;
  padding: 1.5rem;
}

/* =============================================
   TAB BAR
   ============================================= */
.tab-bar {
  display: flex;
  gap: 0;
  margin-bottom: 1.5rem;
  border-bottom: 2px solid #21262d;
}

.tab-btn {
  background: transparent;
  color: #8b949e;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -2px;
  padding: 0.65rem 1.25rem;
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-family: inherit;
}

.tab-btn:hover { color: #c9d1d9; }

.tab-btn.active {
  color: #58a6ff;
  border-bottom-color: #58a6ff;
}

/* =============================================
   LİG SONUÇLARI TAB
   ============================================= */
.lr-filters {
  display: flex;
  gap: 0.85rem;
  align-items: flex-end;
  flex-wrap: wrap;
  margin-bottom: 1.25rem;
}

.result-row { cursor: default; }
.result-row:hover { background: #161b22; }

/* =============================================
   BAŞLIK VE FİLTRELER
   ============================================= */
.page-header {
  margin-bottom: 1.5rem;
}

.header-title-section {
  margin-bottom: 1.25rem;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 800;
  color: #ffffff;
  margin: 0 0 0.35rem 0;
}

.page-desc {
  font-size: 0.85rem;
  color: #8b949e;
  margin: 0;
}

.header-filters {
  display: flex;
  gap: 0.85rem;
  align-items: flex-end;
  flex-wrap: wrap;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.league-filter-group {
  flex: 1;
  min-width: 180px;
}

.filter-label {
  font-size: 0.7rem;
  font-weight: 600;
  color: #8b949e;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.filter-select {
  background: #161b22;
  color: #c9d1d9;
  border: 1px solid #30363d;
  padding: 0.55rem 0.85rem;
  border-radius: 8px;
  font-size: 0.85rem;
  cursor: pointer;
  outline: none;
  transition: border-color 0.2s;
  min-width: 140px;
}

.filter-select:focus { border-color: #58a6ff; }
.filter-select option { background: #161b22; }
.filter-select option.league-opt-heading {
  color: #58a6ff;
  font-weight: 600;
  font-size: 0.75rem;
  background: #0d1117;
}

.league-select { min-width: 200px; }

/* =============================================
   TARİH SEÇİCİ
   ============================================= */
.date-picker-wrapper {
  display: flex;
  align-items: center;
  gap: 0;
}

.date-nav-btn {
  background: #21262d;
  border: 1px solid #30363d;
  color: #c9d1d9;
  padding: 0.55rem 0.65rem;
  font-size: 1.1rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
  line-height: 1;
}

.date-nav-btn:first-child {
  border-radius: 8px 0 0 8px;
}

.date-nav-btn:last-child {
  border-radius: 0 8px 8px 0;
}

.date-nav-btn + .date-nav-btn,
.date-nav-btn + .date-input,
.date-input + .date-nav-btn {
  border-left: none;
}

.date-nav-btn.double {
  font-size: 0.85rem;
  color: #58a6ff;
  padding: 0.55rem 0.5rem;
}

.date-nav-btn:hover:not(:disabled) {
  background: #30363d;
  color: #58a6ff;
}

.date-nav-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.date-input {
  background: #161b22;
  color: #c9d1d9;
  border: 1px solid #30363d;
  padding: 0.55rem 0.75rem;
  font-size: 0.85rem;
  outline: none;
  transition: border-color 0.2s;
  min-width: 145px;
}

.date-input:focus { border-color: #58a6ff; }
.date-input::-webkit-calendar-picker-indicator { filter: invert(0.7); cursor: pointer; }

/* =============================================
   ARA BUTONU
   ============================================= */
.search-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  background: #27AE60;
  color: #ffffff;
  border: 1px solid #27AE60;
  padding: 0.55rem 1.25rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
  min-width: 90px;
  min-height: 38px;
}

.search-btn:hover:not(:disabled) {
  background: #27AE60;
}

.search-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

/* =============================================
   HIZLI TARİH KISAYOLLARI
   ============================================= */
.quick-dates {
  display: flex;
  gap: 0.4rem;
  flex-wrap: wrap;
  margin-top: 0.85rem;
}

.quick-date-btn {
  background: #161b22;
  color: #8b949e;
  border: 1px solid #21262d;
  padding: 0.35rem 0.75rem;
  border-radius: 16px;
  font-size: 0.75rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.quick-date-btn:hover {
  background: #21262d;
  color: #c9d1d9;
  border-color: #30363d;
}

.quick-date-btn.active {
  background: #58a6ff22;
  color: #58a6ff;
  border-color: #58a6ff55;
}

.spinner-xs {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

/* =============================================
   TARİH BİLGİ ÇUBUĞU
   ============================================= */
.date-info-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.6rem 1rem;
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.date-info-text {
  font-size: 0.85rem;
  color: #c9d1d9;
  font-weight: 500;
}

.match-count-badge {
  background: #58a6ff18;
  color: #58a6ff;
  padding: 0.2rem 0.65rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
}

/* =============================================
   YÜKLENİYOR, HATA, BOŞ DURUMLAR
   ============================================= */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 4rem;
  color: #8b949e;
  font-size: 0.9rem;
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

.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 4rem;
  text-align: center;
}

.error-icon { font-size: 2.5rem; }
.error-state p { color: #f85149; font-size: 0.95rem; font-weight: 500; }

.retry-btn {
  background: #21262d;
  color: #58a6ff;
  border: 1px solid #30363d;
  padding: 0.5rem 1.25rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.retry-btn:hover { background: #30363d; }

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 4rem;
  color: #8b949e;
  text-align: center;
}

.empty-icon { font-size: 2.5rem; opacity: 0.5; }
.empty-state p { font-size: 1rem; font-weight: 500; margin: 0; }
.empty-state small { font-size: 0.8rem; color: #484f58; }

.initial-state .empty-icon { opacity: 0.3; font-size: 3rem; }

/* =============================================
   MAÇ LİSTESİ
   ============================================= */
.matches-container {
  border: 1px solid #21262d;
  border-radius: 10px;
  overflow: hidden;
}

.league-group {
  border-bottom: 1px solid #21262d;
}

.league-group:last-child { border-bottom: none; }

.league-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.6rem 1rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.league-title {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.league-icon { font-size: 0.85rem; }

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

.league-match-count {
  font-size: 0.7rem;
  color: #484f58;
  font-weight: 500;
}

/* =============================================
   MAÇ SATIRI
   ============================================= */
.match-row {
  display: flex;
  align-items: center;
  padding: 0.65rem 1rem;
  cursor: pointer;
  transition: background 0.15s;
  border-bottom: 1px solid #21262d10;
}

.match-row:hover { background: #1c2129; }
.match-row:last-child { border-bottom: none; }

.match-time-col {
  width: 65px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.2rem;
  flex-shrink: 0;
}

.match-clock {
  font-size: 0.8rem;
  color: #8b949e;
  font-weight: 500;
}

.status-badge {
  font-size: 0.65rem;
  font-weight: 600;
  padding: 0.1rem 0.35rem;
  border-radius: 3px;
}

.status-badge.finished { color: #8b949e; background: #21262d; }
.status-badge.live { color: #ffffff; background: #f85149; }
.status-badge.halftime { color: #ffffff; background: #da3633; }
.status-badge.not-started { color: #8b949e; background: transparent; }

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
.team-clickable {
  cursor: pointer;
  border-radius: 4px;
  padding: 0.1rem 0.2rem;
  margin: -0.1rem -0.2rem;
  transition: background 0.15s;
}
.team-clickable:hover { background: #30363d; }

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

.match-detail-col {
  width: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.detail-arrow {
  color: #30363d;
  font-size: 0.9rem;
  transition: color 0.2s;
}

.match-row:hover .detail-arrow { color: #58a6ff; }

/* =============================================
   RESPONSIVE
   ============================================= */
@media (max-width: 768px) {
  .past-matches-page { padding: 1rem; }

  .header-filters {
    flex-direction: column;
    gap: 0.65rem;
  }

  .filter-group { width: 100%; }

  .league-filter-group { min-width: unset; }

  .filter-select,
  .league-select {
    width: 100%;
    min-width: unset;
  }

  .date-picker-wrapper { width: 100%; }
  .date-input { flex: 1; }

  .search-btn { width: 100%; }

  .quick-dates { gap: 0.3rem; }
  .quick-date-btn { padding: 0.3rem 0.6rem; font-size: 0.7rem; }
}

@media (max-width: 480px) {
  .page-title { font-size: 1.2rem; }
  .match-row { padding: 0.5rem 0.75rem; }
  .match-time-col { width: 55px; }
}
</style>
