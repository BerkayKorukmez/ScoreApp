<template>
  <div class="fixtures-page">

    <!-- ── Başlık ──────────────────────────────────────────────────── -->
    <div class="page-header">
      <h1 class="page-title">
        <span class="icon">📅</span> Fikstür
      </h1>
      <p class="page-subtitle">Takım arayarak sezon fikstürüne ulaş</p>
    </div>

    <!-- ── Arama Paneli ────────────────────────────────────────────── -->
    <div class="search-panel">

      <!-- Arama kutusu -->
      <div class="search-box-wrapper">
        <div class="search-box">
          <span class="search-icon">🔍</span>
          <input
            v-model="query"
            placeholder="Takım ara... (ör: Galatasaray)"
            class="search-input"
            @input="onQueryInput"
            @keydown.escape="clearSearch"
          />
          <button v-if="query" class="clear-btn" @click="clearSearch">✕</button>
        </div>

        <!-- Arama sonuçları dropdown -->
        <div v-if="searchResults.length > 0 && !selectedItem" class="search-dropdown">
          <button
            v-for="item in searchResults"
            :key="item.id"
            class="search-result-item"
            @click="selectItem(item)"
          >
            <img
              v-if="item.logo || item.flag"
              :src="item.logo || item.flag"
              class="result-logo"
              @error="e => e.target.style.display='none'"
            />
            <div v-else class="result-logo-placeholder">?</div>
            <div class="result-info">
              <span class="result-name">{{ item.name }}</span>
              <span v-if="item.country" class="result-country">{{ item.country }}</span>
            </div>
          </button>
        </div>

        <!-- Arama yükleniyor -->
        <div v-if="isSearching" class="search-loading">Aranıyor...</div>

        <!-- Sonuç yok -->
        <div v-if="query.length >= 2 && !isSearching && searchResults.length === 0 && !selectedItem" class="search-empty">
          Sonuç bulunamadı
        </div>
      </div>
    </div>

    <!-- ── Seçilen öğe bilgisi ─────────────────────────────────────── -->
    <div v-if="selectedItem" class="selected-banner">
      <img
        v-if="selectedItem.logo || selectedItem.flag"
        :src="selectedItem.logo || selectedItem.flag"
        class="banner-logo"
        @error="e => e.target.style.display='none'"
      />
      <div class="banner-info">
        <h2 class="banner-name">{{ selectedItem.name }}</h2>
        <span v-if="selectedItem.country" class="banner-country">{{ selectedItem.country }}</span>
      </div>
      <div class="banner-meta">
        <span class="banner-sport">{{ currentSportLabel }}</span>
        <span class="banner-season">{{ displaySeason }} Sezonu</span>
      </div>
      <button class="banner-change" @click="clearSearch">Değiştir</button>
    </div>

    <!-- ── Fikstür listesi ────────────────────────────────────────── -->
    <div v-if="selectedItem" class="fixtures-content">

      <!-- Yükleniyor -->
      <div v-if="isLoadingFixtures" class="loading-state">
        <div class="spinner"></div>
        <p>Fikstür yükleniyor...</p>
      </div>

      <!-- Boş -->
      <div v-else-if="!isLoadingFixtures && fixtures.length === 0" class="empty-state">
        <div class="empty-icon">📭</div>
        <h3>Fikstür bulunamadı</h3>
        <p>Bu sezon için maç verisi mevcut değil.</p>
      </div>

      <template v-else>
        <!-- Özet istatistikler -->
        <div class="stats-row">
          <div class="stat-card">
            <span class="stat-num">{{ stats.total }}</span>
            <span class="stat-label">Toplam Maç</span>
          </div>
          <div class="stat-card finished">
            <span class="stat-num">{{ stats.finished }}</span>
            <span class="stat-label">Oynandı</span>
          </div>
          <div class="stat-card live">
            <span class="stat-num">{{ stats.live }}</span>
            <span class="stat-label">Canlı</span>
          </div>
          <div class="stat-card upcoming">
            <span class="stat-num">{{ stats.upcoming }}</span>
            <span class="stat-label">Oynanacak</span>
          </div>
        </div>

        <!-- Ay filtreleri -->
        <div class="month-filters">
          <button
            :class="['month-btn', { active: selectedMonth === null }]"
            @click="selectedMonth = null"
          >Tümü</button>
          <button
            v-for="m in availableMonths"
            :key="m.value"
            :class="['month-btn', { active: selectedMonth === m.value }]"
            @click="selectedMonth = m.value"
          >{{ m.label }}</button>
        </div>

        <!-- Tamamlananlar -->
        <div v-if="filteredFinished.length > 0" class="fixture-section">
          <div class="section-header finished-header">
            <span class="section-icon">✅</span>
            <h3>Oynandı ({{ filteredFinished.length }})</h3>
          </div>
          <div class="fixture-list">
            <FixtureRow
              v-for="match in filteredFinished"
              :key="match.id"
              :match="match"
              :sport="sport"
            />
          </div>
        </div>

        <!-- Canlı -->
        <div v-if="filteredLive.length > 0" class="fixture-section">
          <div class="section-header live-header">
            <span class="section-icon live-dot"></span>
            <h3>Canlı ({{ filteredLive.length }})</h3>
          </div>
          <div class="fixture-list">
            <FixtureRow
              v-for="match in filteredLive"
              :key="match.id"
              :match="match"
              :sport="sport"
              :highlight="true"
            />
          </div>
        </div>

        <!-- Oynanacaklar -->
        <div v-if="filteredUpcoming.length > 0" class="fixture-section">
          <div class="section-header upcoming-header">
            <span class="section-icon">🗓️</span>
            <h3>Oynanacak ({{ filteredUpcoming.length }})</h3>
          </div>
          <div class="fixture-list">
            <FixtureRow
              v-for="match in filteredUpcoming"
              :key="match.id"
              :match="match"
              :sport="sport"
            />
          </div>
        </div>

      </template>
    </div>

    <!-- ── Hoşgeldin ekranı (henüz seçim yok) ───────────────────── -->
    <div v-else-if="!selectedItem && query.length === 0" class="welcome-screen">
      <div class="welcome-icon">⚽🏀🏐</div>
      <h2>Fikstür Arama</h2>
      <p>Yukarıdan bir takım arayarak o sezonun tüm maçlarını görüntüleyin.</p>
      <div class="quick-teams">
        <p class="quick-label">Popüler takımlar:</p>
        <div class="quick-list">
          <button
            v-for="t in quickTeams"
            :key="t.id"
            class="quick-item"
            @click="quickSelect(t)"
          >
            <img :src="t.logo" @error="e => e.target.style.display='none'" class="quick-logo" />
            {{ t.name }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { searchFixture, fetchFixturesByTeam } from '../api/fixtureApi'
import FixtureRow from '../components/fixture/FixtureRow.vue'
import { FOOTBALL_POPULAR_QUICK_TEAMS, footballTeamLogoUrl } from '../constants/footballPopularTeams'

// ── Sabitler ──────────────────────────────────────────────────────────────────
const sports = [
  { id: 'football', label: 'Futbol', icon: '⚽' }
]

const quickTeams = FOOTBALL_POPULAR_QUICK_TEAMS.map((t) => ({
  ...t,
  logo: footballTeamLogoUrl(t.id)
}))

// ── State ────────────────────────────────────────────────────────────────────
const sport         = ref('football')
const query         = ref('')
const searchResults = ref([])
const selectedItem  = ref(null)
const isSearching   = ref(false)
const isLoadingFixtures = ref(false)
const fixtures      = ref([])
const selectedMonth = ref(null)

let searchTimer = null

// ── Computed ─────────────────────────────────────────────────────────────────
const currentSportLabel = computed(() =>
  sports.find(s => s.id === sport.value)?.label ?? ''
)

const displaySeason = computed(() => {
  const now = new Date()
  const year = now.getFullYear()
  const base = now.getMonth() < 7 ? year - 1 : year
  return String(base)
})

// status: 0=NotStarted, 1=Live, 2=HalfTime, 3=Finished
const stats = computed(() => ({
  total:    fixtures.value.length,
  finished: fixtures.value.filter(m => m.status === 3).length,
  live:     fixtures.value.filter(m => m.status === 1 || m.status === 2).length,
  upcoming: fixtures.value.filter(m => m.status === 0).length
}))

const availableMonths = computed(() => {
  const months = new Map()
  fixtures.value.forEach(m => {
    const d = new Date(m.startTime)
    const val = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    const label = d.toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' })
    if (!months.has(val)) months.set(val, label)
  })
  return Array.from(months.entries())
    .map(([value, label]) => ({ value, label }))
    .sort((a, b) => a.value.localeCompare(b.value))
})

const monthFilter = (matches) => {
  if (!selectedMonth.value) return matches
  return matches.filter(m => {
    const d = new Date(m.startTime)
    const val = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    return val === selectedMonth.value
  })
}

const filteredFinished = computed(() =>
  monthFilter(fixtures.value.filter(m => m.status === 3))
    .sort((a, b) => new Date(b.startTime) - new Date(a.startTime))
)
const filteredLive = computed(() =>
  monthFilter(fixtures.value.filter(m => m.status === 1 || m.status === 2))
)
const filteredUpcoming = computed(() =>
  monthFilter(fixtures.value.filter(m => m.status === 0))
    .sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
)

// ── Metodlar ──────────────────────────────────────────────────────────────────
const onQueryInput = () => {
  selectedItem.value = null
  clearTimeout(searchTimer)
  if (query.value.length < 2) { searchResults.value = []; return }
  isSearching.value = true
  searchTimer = setTimeout(async () => {
    try {
      searchResults.value = await searchFixture(query.value, sport.value, 'team')
    } finally {
      isSearching.value = false
    }
  }, 400)
}

const selectItem = async (item) => {
  selectedItem.value  = item
  query.value         = item.name
  searchResults.value = []
  await loadFixtures(item)
}

const quickSelect = async (t) => {
  sport.value        = t.sport || 'football'
  selectedItem.value = { id: t.id, name: t.name, logo: t.logo, kind: 'team' }
  query.value        = t.name
  await loadFixtures(selectedItem.value)
}

const loadFixtures = async (item) => {
  isLoadingFixtures.value = true
  fixtures.value          = []
  selectedMonth.value     = null
  try {
    fixtures.value = await fetchFixturesByTeam(item.id, sport.value)
  } finally {
    isLoadingFixtures.value = false
  }
}

const clearSearch = () => {
  query.value         = ''
  searchResults.value = []
  selectedItem.value  = null
  fixtures.value      = []
  selectedMonth.value = null
}
</script>

<style scoped>
/* ── Sayfa ── */
.fixtures-page {
  max-width: 960px;
  margin: 0 auto;
  padding: 24px 16px 60px;
  color: #e6edf3;
}

.page-header {
  text-align: center;
  margin-bottom: 28px;
}
.page-title {
  font-size: 28px;
  font-weight: 700;
  margin: 0 0 6px;
}
.page-title .icon { font-size: 26px; }
.page-subtitle {
  font-size: 14px;
  color: #8b949e;
  margin: 0;
}

/* ── Arama paneli ── */
.search-panel {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 20px;
}

/* Arama kutusu */
.search-box-wrapper { position: relative; }
.search-box {
  display: flex;
  align-items: center;
  gap: 10px;
  background: #0d1117;
  border: 1px solid #30363d;
  border-radius: 8px;
  padding: 10px 14px;
  transition: border-color .18s;
}
.search-box:focus-within { border-color: #58a6ff; }
.search-icon { font-size: 16px; color: #8b949e; }
.search-input {
  flex: 1;
  background: transparent;
  border: none;
  outline: none;
  color: #e6edf3;
  font-size: 15px;
}
.search-input::placeholder { color: #484f58; }
.clear-btn {
  background: transparent;
  border: none;
  color: #8b949e;
  cursor: pointer;
  font-size: 14px;
  padding: 2px 4px;
}
.clear-btn:hover { color: #e6edf3; }

/* Dropdown */
.search-dropdown {
  position: absolute;
  top: calc(100% + 6px);
  left: 0;
  right: 0;
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 8px;
  z-index: 100;
  max-height: 300px;
  overflow-y: auto;
}
.search-result-item {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 10px 14px;
  background: transparent;
  border: none;
  cursor: pointer;
  color: #e6edf3;
  text-align: left;
  transition: background .15s;
}
.search-result-item:hover { background: #21262d; }
.result-logo {
  width: 32px;
  height: 32px;
  object-fit: contain;
  border-radius: 4px;
  flex-shrink: 0;
}
.result-logo-placeholder {
  width: 32px;
  height: 32px;
  border-radius: 4px;
  background: #21262d;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8b949e;
  font-size: 14px;
  flex-shrink: 0;
}
.result-info { flex: 1; min-width: 0; }
.result-name {
  display: block;
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.result-country { font-size: 12px; color: #8b949e; }
.search-loading { padding: 12px 14px; color: #8b949e; font-size: 13px; }
.search-empty   { padding: 12px 14px; color: #8b949e; font-size: 13px; }

/* ── Seçilen banner ── */
.selected-banner {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 12px;
  padding: 16px 20px;
  margin-bottom: 20px;
}
.banner-logo {
  width: 52px;
  height: 52px;
  object-fit: contain;
  border-radius: 8px;
  flex-shrink: 0;
}
.banner-info { flex: 1; min-width: 0; }
.banner-name {
  font-size: 20px;
  font-weight: 700;
  margin: 0 0 2px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.banner-country { font-size: 13px; color: #8b949e; }
.banner-meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
}
.banner-sport {
  font-size: 12px;
  background: #21262d;
  padding: 2px 10px;
  border-radius: 10px;
  color: #58a6ff;
}
.banner-season { font-size: 12px; color: #8b949e; }
.banner-change {
  padding: 7px 16px;
  border-radius: 8px;
  border: 1px solid #30363d;
  background: transparent;
  color: #8b949e;
  cursor: pointer;
  font-size: 13px;
  transition: all .18s;
  flex-shrink: 0;
}
.banner-change:hover { border-color: #58a6ff; color: #58a6ff; }

/* ── İstatistik kartları ── */
.stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  margin-bottom: 20px;
}
.stat-card {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 10px;
  padding: 14px;
  text-align: center;
}
.stat-card.finished { border-color: #27AE60; }
.stat-card.live     { border-color: #da3633; }
.stat-card.upcoming { border-color: #1f6feb; }
.stat-num   { display: block; font-size: 24px; font-weight: 700; }
.stat-label { font-size: 12px; color: #8b949e; }

/* ── Ay filtreleri ── */
.month-filters {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 20px;
}
.month-btn {
  padding: 5px 14px;
  border-radius: 20px;
  border: 1px solid #30363d;
  background: transparent;
  color: #8b949e;
  cursor: pointer;
  font-size: 12px;
  transition: all .15s;
}
.month-btn:hover  { border-color: #58a6ff; color: #58a6ff; }
.month-btn.active { background: #21262d; border-color: #58a6ff; color: #e6edf3; }

/* ── Fikstür bölümleri ── */
.fixture-section { margin-bottom: 28px; }
.section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 0 8px;
  margin-bottom: 2px;
  border-bottom: 1px solid #21262d;
}
.section-header h3 { font-size: 15px; margin: 0; font-weight: 600; }
.section-icon { font-size: 16px; }
.finished-header h3 { color: #2ECC71; }
.live-header h3     { color: #f85149; }
.upcoming-header h3 { color: #58a6ff; }

.live-dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #f85149;
  animation: pulse 1.4s infinite;
}
@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50%       { opacity: .5; transform: scale(.85); }
}

.fixture-list { display: flex; flex-direction: column; gap: 2px; }

/* ── Loading / Empty ── */
.loading-state {
  text-align: center;
  padding: 60px 20px;
  color: #8b949e;
}
.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #21262d;
  border-top-color: #58a6ff;
  border-radius: 50%;
  animation: spin .8s linear infinite;
  margin: 0 auto 16px;
}
@keyframes spin { to { transform: rotate(360deg); } }

.empty-state {
  text-align: center;
  padding: 60px 20px;
  color: #8b949e;
}
.empty-icon { font-size: 48px; margin-bottom: 12px; }
.empty-state h3 { font-size: 18px; margin: 0 0 6px; color: #e6edf3; }

/* ── Welcome ── */
.welcome-screen {
  text-align: center;
  padding: 60px 20px;
  color: #8b949e;
}
.welcome-icon { font-size: 52px; margin-bottom: 16px; }
.welcome-screen h2  { font-size: 22px; color: #e6edf3; margin: 0 0 8px; }
.welcome-screen > p { font-size: 14px; max-width: 400px; margin: 0 auto 28px; }

.quick-label { font-size: 13px; color: #8b949e; margin-bottom: 12px; }
.quick-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  justify-content: center;
}
.quick-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 8px;
  color: #e6edf3;
  cursor: pointer;
  font-size: 13px;
  transition: all .18s;
}
.quick-item:hover { border-color: #58a6ff; color: #58a6ff; }
.quick-logo {
  width: 22px;
  height: 22px;
  object-fit: contain;
}

/* Responsive */
@media (max-width: 768px) {
  .fixtures-page { padding: 18px 12px 40px; }
  .page-title { font-size: 22px; }
  .search-panel { padding: 14px; }
}

@media (max-width: 600px) {
  .stats-row { grid-template-columns: repeat(2, 1fr); gap: 8px; }
  .stat-num { font-size: 20px; }
  .selected-banner { flex-wrap: wrap; padding: 14px; gap: 12px; }
  .banner-meta { flex-direction: row; align-items: center; width: 100%; }
  .banner-change { margin-left: auto; }
}

@media (max-width: 400px) {
  .page-title { font-size: 19px; }
  .stats-row { grid-template-columns: repeat(2, 1fr); }
}
</style>
