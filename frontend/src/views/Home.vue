<template>
  <div class="app-container">

    <!-- ÜST HEADER ÇUBUĞU -->
    <header class="top-header">
      <div class="header-left">
        <div class="brand">
          <span class="brand-icon">⚽</span>
          <span class="brand-text">SkorTakip</span>
        </div>
        <nav class="header-nav">
          <span class="nav-link active">🏟️ Maçlar</span>
          <router-link to="/news" class="nav-link">📰 Haberler</router-link>
        </nav>
      </div>
      <div class="header-right">
        <template v-if="authStore.isAuthenticated">
          <div class="user-info">
            <span class="user-avatar">👤</span>
            <span class="user-name">{{ authStore.user?.userName || authStore.user?.email || 'Hesabım' }}</span>
          </div>
          <button class="btn-logout" @click="handleLogout">Çıkış</button>
        </template>
        <template v-else>
          <router-link to="/login" class="btn-auth">Giriş Yap</router-link>
          <router-link to="/register" class="btn-auth btn-register">Kayıt Ol</router-link>
        </template>
      </div>
    </header>

    <!-- SPOR SEÇİM ÇUBUĞU -->
    <nav class="sport-nav">
      <button
        v-for="sport in sports"
        :key="sport.id"
        :class="['sport-tab', { active: selectedSport === sport.id }]"
        @click="selectedSport = sport.id"
      >
        <span class="sport-tab-icon">{{ sport.icon }}</span>
        <span class="sport-tab-label">{{ sport.label }}</span>
        <span v-if="getSportMatchCount(sport)" class="sport-tab-count">
          {{ getSportMatchCount(sport) }}
        </span>
      </button>
    </nav>

    <!-- ANA İÇERİK ALANI -->
    <div class="main-layout">

      <!-- SOL PANEL: FİLTRELER VE MAÇ LİSTESİ -->
      <div class="left-panel">

        <!-- Filtre sekmeler -->
        <div class="filter-bar">
          <div class="filter-tabs">
            <button
              :class="['tab-btn', { active: activeFilter === 'live' }]"
              @click="activeFilter = 'live'"
            >
              <span class="tab-dot live-dot"></span>
              Canlı
              <span v-if="liveMatchCount > 0" class="tab-count">{{ liveMatchCount }}</span>
            </button>
            <button
              :class="['tab-btn', { active: activeFilter === 'all' }]"
              @click="activeFilter = 'all'"
            >
              Tümü
            </button>
            <button
              :class="['tab-btn', { active: activeFilter === 'finished' }]"
              @click="activeFilter = 'finished'"
            >
              Tamamlanan
            </button>
            <button
              v-if="authStore.isAuthenticated"
              :class="['tab-btn', { active: activeFilter === 'favorites' }]"
              @click="activeFilter = 'favorites'"
            >
              ⭐ Favoriler
            </button>
          </div>

          <!-- Lig seçimi -->
          <div class="league-selector">
            <select v-model="selectedLeague" class="league-dropdown">
              <option :value="null">Tüm Ligler</option>
              <option
                v-for="league in leagues"
                :key="league"
                :value="league"
              >
                {{ league }}
              </option>
            </select>
          </div>
        </div>

        <!-- Yükleniyor göstergesi -->
        <div v-if="isLoading" class="loading-state">
          <div class="spinner"></div>
          <span>Maçlar yükleniyor...</span>
        </div>

        <!-- Maç bulunamadı durumu -->
        <div v-else-if="displayedMatches.length === 0" class="empty-state">
          <span class="empty-icon">📭</span>
          <p>Maç bulunamadı</p>
          <small>Farklı bir filtre veya lig seçmeyi deneyin</small>
        </div>

        <!-- MAÇLAR LİSTESİ (Liglere göre gruplanmış) -->
        <div v-else class="matches-container">
          <div
            v-for="(group, leagueName) in groupedMatches"
            :key="leagueName"
            class="league-group"
          >
            <!-- Lig başlığı -->
            <div class="league-header">
              <div class="league-title">
                <span class="league-icon">🏆</span>
                <span class="league-name">{{ leagueName }}</span>
              </div>
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
                <div class="team-row">
                  <span class="team-name" :class="{ winner: match.status === 3 && match.homeScore > match.awayScore }">
                    {{ match.homeTeam }}
                  </span>
                  <span class="team-score" :class="{ 'is-live': match.status === 1 }">
                    {{ match.homeScore }}
                  </span>
                </div>
                <div class="team-row">
                  <span class="team-name" :class="{ winner: match.status === 3 && match.awayScore > match.homeScore }">
                    {{ match.awayTeam }}
                  </span>
                  <span class="team-score" :class="{ 'is-live': match.status === 1 }">
                    {{ match.awayScore }}
                  </span>
                </div>
              </div>

              <!-- Favori butonu -->
              <div class="match-actions-col">
                <button
                  v-if="authStore.isAuthenticated"
                  class="fav-btn"
                  :class="{ active: isFavorite(match.id) }"
                  @click.stop="toggleFavorite(match)"
                  title="Favorilere ekle / çıkar"
                >
                  ★
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- SAĞ PANEL: PUAN TABLOSU -->
      <aside class="right-panel">
        <!-- Lig seçilmemişse bilgilendirme -->
        <div v-if="!selectedLeague" class="standings-placeholder">
          <span class="placeholder-icon">📊</span>
          <p class="placeholder-title">Puan Tablosu</p>
          <p class="placeholder-desc">Bir lig seçerek o ligin puan tablosunu görüntüleyebilirsiniz</p>
        </div>

        <!-- Lig seçiliyse puan tablosu -->
        <div v-else class="standings-panel">
          <div class="standings-header">
            <span class="standings-league-icon">🏆</span>
            <h3 class="standings-title">{{ selectedLeague }}</h3>
          </div>

          <!-- Puan tablosu verisi yoksa -->
          <div v-if="leagueStandings.length === 0" class="standings-empty">
            <p>Bu lig için yeterli veri bulunamadı</p>
            <small>Tamamlanan maçlar üzerinden hesaplanır</small>
          </div>

          <!-- Puan tablosu -->
          <table v-else class="standings-table">
            <thead>
              <tr>
                <th class="col-pos">#</th>
                <th class="col-team">Takım</th>
                <th class="col-stat">O</th>
                <th class="col-stat">G</th>
                <th class="col-stat">B</th>
                <th class="col-stat">M</th>
                <th class="col-stat">A</th>
                <th class="col-stat">Y</th>
                <th class="col-stat">AV</th>
                <th class="col-pts">P</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(team, index) in leagueStandings"
                :key="team.name"
                :class="getStandingRowClass(index)"
              >
                <td class="col-pos">{{ index + 1 }}</td>
                <td class="col-team">{{ team.name }}</td>
                <td class="col-stat">{{ team.played }}</td>
                <td class="col-stat">{{ team.won }}</td>
                <td class="col-stat">{{ team.drawn }}</td>
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

          <!-- Son 5 maç formu açıklaması -->
          <div class="standings-legend">
            <div class="legend-item">
              <span class="legend-color champ"></span> Şampiyonlar Ligi
            </div>
            <div class="legend-item">
              <span class="legend-color europa"></span> Avrupa Ligi
            </div>
            <div class="legend-item">
              <span class="legend-color relegation"></span> Düşme hattı
            </div>
          </div>
        </div>
      </aside>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { fetchMatches, fetchFavoriteMatchIds, addFavoriteMatch, removeFavoriteMatch } from '../api/matchApi'
import * as signalR from '@microsoft/signalr'

const router = useRouter()
const authStore = useAuthStore()

/* =============================================
   DURUM DEĞİŞKENLERİ (State)
   ============================================= */
const selectedSport = ref('football')
const selectedLeague = ref(null)
const activeFilter = ref('all')
const isLoading = ref(false)
const favoriteMatchIds = ref([])
const matches = ref([])

/* =============================================
   SABİT VERİLER
   ============================================= */

// Desteklenen spor tipleri (backend SportType enum değerleriyle eşleşir)
const sports = [
  { id: 'football',   icon: '⚽', label: 'Futbol',     sportType: 0 },
  { id: 'basketball', icon: '🏀', label: 'Basketbol',  sportType: 1 },
  { id: 'volleyball', icon: '🏐', label: 'Voleybol',   sportType: 3 }
]

// Popüler ligler — dropdown'da önce bunlar gösterilir
const popularLeagues = [
  'Süper Lig', 'Premier League', 'La Liga', 'Serie A',
  'Bundesliga', 'Ligue 1', 'UEFA Champions League',
  'UEFA Europa League', 'Eredivisie', 'MLS'
]

/* =============================================
   HESAPLANAN ÖZELLİKLER (Computed)
   ============================================= */

// Seçili spora ait ligleri döndürür (popüler olanlar üstte)
const leagues = computed(() => {
  const selectedSportData = sports.find(s => s.id === selectedSport.value)
  if (!selectedSportData) return []

  const sportMatches = matches.value.filter(m => m.sportType === selectedSportData.sportType)
  const set = new Set()
  sportMatches.forEach(m => {
    if (m.league) set.add(m.league)
  })

  const allLeagues = Array.from(set)
  return allLeagues.sort((a, b) => {
    const ia = popularLeagues.indexOf(a)
    const ib = popularLeagues.indexOf(b)
    if (ia !== -1 && ib !== -1) return ia - ib
    if (ia !== -1) return -1
    if (ib !== -1) return 1
    return a.localeCompare(b, 'tr-TR')
  })
})

// Canlı maç sayısını döndürür (filtre sekmesinde gösterilir)
const liveMatchCount = computed(() => {
  const selectedSportData = sports.find(s => s.id === selectedSport.value)
  if (!selectedSportData) return 0
  return matches.value.filter(
    m => m.sportType === selectedSportData.sportType && m.status === 1
  ).length
})

// Bir spor kategorisindeki toplam maç sayısını döndürür
const getSportMatchCount = (sport) => {
  return matches.value.filter(m => m.sportType === sport.sportType).length
}

// Aktif filtrelere göre gösterilecek maçları hesaplar
const displayedMatches = computed(() => {
  let filtered = matches.value
  const selectedSportData = sports.find(s => s.id === selectedSport.value)

  // Seçili spora göre filtrele
  if (selectedSportData) {
    filtered = filtered.filter(m => m.sportType === selectedSportData.sportType)
  }

  // Seçili lige göre filtrele
  if (selectedLeague.value) {
    filtered = filtered.filter(m => m.league === selectedLeague.value)
  }

  // Durum filtreleri
  if (activeFilter.value === 'live') {
    filtered = filtered.filter(m => m.status === 1)
  } else if (activeFilter.value === 'finished') {
    filtered = filtered.filter(m => m.status === 3)
  } else if (activeFilter.value === 'favorites') {
    filtered = filtered.filter(m => favoriteMatchIds.value.includes(m.id))
  }

  // Sıralama: canlı maçlar üstte, sonra başlangıç zamanına göre
  return filtered.sort((a, b) => {
    if (a.status === 1 && b.status !== 1) return -1
    if (a.status !== 1 && b.status === 1) return 1
    return new Date(b.startTime) - new Date(a.startTime)
  })
})

// Maçları lig ismine göre gruplar (sol panelde liglere göre gösterim için)
const groupedMatches = computed(() => {
  const groups = {}
  displayedMatches.value.forEach(match => {
    const league = match.league || 'Diğer'
    if (!groups[league]) groups[league] = []
    groups[league].push(match)
  })
  return groups
})

// Seçili ligin puan tablosunu hesaplar (tamamlanan maçlardan)
const leagueStandings = computed(() => {
  if (!selectedLeague.value) return []

  const selectedSportData = sports.find(s => s.id === selectedSport.value)
  if (!selectedSportData) return []

  // Seçili lig ve spor tipine ait tamamlanan maçları al
  const finishedMatches = matches.value.filter(
    m => m.league === selectedLeague.value &&
         m.sportType === selectedSportData.sportType &&
         m.status === 3
  )

  if (finishedMatches.length === 0) return []

  // Takım istatistiklerini hesapla
  const teamStats = {}

  const getOrCreate = (name) => {
    if (!teamStats[name]) {
      teamStats[name] = {
        name,
        played: 0,     // Oynanan maç
        won: 0,         // Kazanma
        drawn: 0,       // Beraberlik
        lost: 0,        // Mağlubiyet
        goalsFor: 0,    // Atılan gol
        goalsAgainst: 0, // Yenilen gol
        goalDiff: 0,    // Averaj
        points: 0       // Puan
      }
    }
    return teamStats[name]
  }

  finishedMatches.forEach(match => {
    const home = getOrCreate(match.homeTeam)
    const away = getOrCreate(match.awayTeam)

    // Oynanan maç sayısı
    home.played++
    away.played++

    // Gol istatistikleri
    home.goalsFor += match.homeScore
    home.goalsAgainst += match.awayScore
    away.goalsFor += match.awayScore
    away.goalsAgainst += match.homeScore

    // Sonuca göre puan dağılımı (Galibiyet=3, Beraberlik=1, Mağlubiyet=0)
    if (match.homeScore > match.awayScore) {
      home.won++
      home.points += 3
      away.lost++
    } else if (match.homeScore < match.awayScore) {
      away.won++
      away.points += 3
      home.lost++
    } else {
      home.drawn++
      away.drawn++
      home.points += 1
      away.points += 1
    }

    // Averaj güncelle
    home.goalDiff = home.goalsFor - home.goalsAgainst
    away.goalDiff = away.goalsFor - away.goalsAgainst
  })

  // Sıralama: Puan > Averaj > Atılan Gol
  return Object.values(teamStats).sort((a, b) => {
    if (b.points !== a.points) return b.points - a.points
    if (b.goalDiff !== a.goalDiff) return b.goalDiff - a.goalDiff
    return b.goalsFor - a.goalsFor
  })
})

/* =============================================
   YARDIMCI FONKSİYONLAR
   ============================================= */

// Maç saatini biçimlendir (ör: "20:45")
const formatTime = (dateString) => {
  const date = new Date(dateString)
  return date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

// Maç detay sayfasına yönlendir
const goToMatchDetail = (matchId) => {
  router.push(`/match/${matchId}`)
}

// Oturumu kapat
const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

// Maçın favorilerde olup olmadığını kontrol et
const isFavorite = (matchId) => {
  return favoriteMatchIds.value.includes(matchId)
}

// Puan tablosundaki satır rengini belirle (şampiyon hattı, küme düşme vb.)
const getStandingRowClass = (index) => {
  const total = leagueStandings.value.length
  if (total <= 3) return '' // Az takım varsa renklendirme yapma
  if (index < 1) return 'row-champion'
  if (index < 3) return 'row-europa'
  if (index >= total - 2) return 'row-relegation'
  return ''
}

/* =============================================
   API VE FAVORİ İŞLEMLERİ
   ============================================= */

// Favori maç listesini sunucudan yükle
const loadFavoriteMatches = async () => {
  if (!authStore.isAuthenticated) {
    favoriteMatchIds.value = []
    return
  }
  try {
    favoriteMatchIds.value = await fetchFavoriteMatchIds()
  } catch (error) {
    console.warn('Favori maçlar yüklenemedi:', error.response?.status)
    favoriteMatchIds.value = []
  }
}

// Favori durumunu değiştir (ekle / çıkar)
const toggleFavorite = async (match) => {
  if (!authStore.isAuthenticated) return
  const id = match.id
  if (!id) return

  if (isFavorite(id)) {
    try {
      await removeFavoriteMatch(id)
      favoriteMatchIds.value = favoriteMatchIds.value.filter(x => x !== id)
    } catch (error) {
      console.error('Favori kaldırılamadı:', error)
    }
  } else {
    try {
      await addFavoriteMatch(id)
      favoriteMatchIds.value.push(id)
    } catch (error) {
      console.error('Favoriye eklenemedi:', error)
    }
  }
}

// İlk yüklemede API'den maçları çek
const fetchInitialMatches = async () => {
  isLoading.value = true
  try {
    matches.value = await fetchMatches(selectedSport.value)
    console.log(`Maçlar yüklendi: ${matches.value.length} adet (${selectedSport.value})`)
  } catch (error) {
    console.error('Maçlar yüklenemedi:', error.response?.status, error.message)
    matches.value = []
  } finally {
    isLoading.value = false
  }
}

/* =============================================
   WEBSOCKET BAĞLANTISI (SignalR)
   ============================================= */
let wsConnection = null

const setupWebSocket = () => {
  wsConnection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/matchhub')
    .withAutomaticReconnect()
    .build()

  // Sunucu cache'ten tüm maçları gönderdiğinde
  wsConnection.on('AllMatches', (allMatches) => {
    if (Array.isArray(allMatches) && allMatches.length > 0) {
      allMatches.forEach(match => {
        const index = matches.value.findIndex(m => m.id === match.id)
        if (index !== -1) {
          matches.value[index] = match
        } else {
          matches.value.push(match)
        }
      })
      console.log(`WebSocket: ${allMatches.length} maç alındı (cache)`)
    }
  })

  // Tekil maç güncellemesi
  wsConnection.on('MatchUpdated', (match) => {
    const index = matches.value.findIndex(m => m.id === match.id)
    if (index !== -1) {
      matches.value.splice(index, 1, match)
    } else {
      matches.value.push(match)
    }
    console.log(`WebSocket: ${match.homeTeam} ${match.homeScore}-${match.awayScore} ${match.awayTeam}`)
  })

  // Maç silindi
  wsConnection.on('MatchRemoved', (matchId) => {
    matches.value = matches.value.filter(m => m.id !== matchId)
  })

  wsConnection.start()
    .then(() => console.log('WebSocket bağlantısı kuruldu ✓'))
    .catch(err => console.error('WebSocket bağlantı hatası:', err))
}

const closeWebSocket = () => {
  if (wsConnection) {
    wsConnection.stop()
    wsConnection = null
  }
}

/* =============================================
   YAŞAM DÖNGÜSÜ (Lifecycle)
   ============================================= */

// Spor değiştiğinde ligi sıfırla ve maçları yeniden yükle
watch(selectedSport, () => {
  selectedLeague.value = null
  fetchInitialMatches()
})

onMounted(() => {
  setupWebSocket()
  fetchInitialMatches()
  loadFavoriteMatches()
})

onUnmounted(() => {
  closeWebSocket()
})
</script>

<style scoped>
/* =============================================
   GENEL KONTEYNER
   ============================================= */
.app-container {
  min-height: 100vh;
  background: #0e1118;
  color: #e1e4e8;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

/* =============================================
   ÜST HEADER
   ============================================= */
.top-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1.5rem;
  height: 56px;
  background: #161b22;
  border-bottom: 1px solid #21262d;
  position: sticky;
  top: 0;
  z-index: 100;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.brand-icon {
  font-size: 1.4rem;
}

.brand-text {
  font-size: 1.2rem;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: -0.5px;
}

.header-nav {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  margin-left: 1.5rem;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.35rem 0.85rem;
  color: #8b949e;
  text-decoration: none;
  font-size: 0.82rem;
  font-weight: 500;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.nav-link:hover {
  color: #c9d1d9;
  background: #21262d;
}

.nav-link.active {
  color: #58a6ff;
  background: #58a6ff15;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.75rem;
  background: #21262d;
  border-radius: 20px;
}

.user-avatar {
  font-size: 0.85rem;
}

.user-name {
  font-size: 0.8rem;
  font-weight: 500;
  color: #c9d1d9;
}

.btn-logout {
  background: transparent;
  color: #f85149;
  border: 1px solid #f8514933;
  padding: 0.35rem 0.85rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-logout:hover {
  background: #f8514922;
  border-color: #f85149;
}

.btn-auth {
  background: transparent;
  color: #58a6ff;
  border: 1px solid #58a6ff33;
  padding: 0.35rem 0.85rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  text-decoration: none;
  transition: all 0.2s;
}

.btn-auth:hover {
  background: #58a6ff22;
}

.btn-register {
  background: #238636;
  color: #ffffff;
  border-color: #238636;
}

.btn-register:hover {
  background: #2ea043;
}

/* =============================================
   SPOR SEÇİM ÇUBUĞU
   ============================================= */
.sport-nav {
  display: flex;
  align-items: center;
  gap: 0;
  background: #161b22;
  border-bottom: 1px solid #21262d;
  padding: 0 1.5rem;
  overflow-x: auto;
}

.sport-tab {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.75rem 1.25rem;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: #8b949e;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.sport-tab:hover {
  color: #c9d1d9;
  background: #1c2129;
}

.sport-tab.active {
  color: #58a6ff;
  border-bottom-color: #58a6ff;
}

.sport-tab-icon {
  font-size: 1.05rem;
}

.sport-tab-count {
  background: #30363d;
  color: #8b949e;
  padding: 0.1rem 0.45rem;
  border-radius: 10px;
  font-size: 0.7rem;
  font-weight: 600;
}

.sport-tab.active .sport-tab-count {
  background: #58a6ff33;
  color: #58a6ff;
}

/* =============================================
   ANA LAYOUT (İki Sütunlu)
   ============================================= */
.main-layout {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 0;
  max-width: 1400px;
  margin: 0 auto;
  min-height: calc(100vh - 112px);
}

/* =============================================
   SOL PANEL: FİLTRE VE MAÇLAR
   ============================================= */
.left-panel {
  border-right: 1px solid #21262d;
  overflow-y: auto;
}

.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1.25rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
  position: sticky;
  top: 0;
  z-index: 10;
}

.filter-tabs {
  display: flex;
  gap: 0.25rem;
}

.tab-btn {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.85rem;
  background: transparent;
  border: 1px solid transparent;
  color: #8b949e;
  font-size: 0.8rem;
  font-weight: 500;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.tab-btn:hover {
  color: #c9d1d9;
  background: #21262d;
}

.tab-btn.active {
  color: #ffffff;
  background: #21262d;
  border-color: #30363d;
}

.tab-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.live-dot {
  background: #f85149;
  box-shadow: 0 0 6px #f8514966;
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.tab-count {
  background: #f8514933;
  color: #f85149;
  padding: 0.05rem 0.4rem;
  border-radius: 8px;
  font-size: 0.7rem;
  font-weight: 700;
}

.league-selector {
  flex-shrink: 0;
}

.league-dropdown {
  background: #0d1117;
  color: #c9d1d9;
  border: 1px solid #30363d;
  padding: 0.4rem 0.75rem;
  border-radius: 6px;
  font-size: 0.8rem;
  cursor: pointer;
  min-width: 160px;
  outline: none;
  transition: border-color 0.2s;
}

.league-dropdown:focus {
  border-color: #58a6ff;
}

.league-dropdown option {
  background: #161b22;
}

/* =============================================
   YÜKLENİYOR VE BOŞ DURUM
   ============================================= */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 3rem;
  color: #8b949e;
  font-size: 0.9rem;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #21262d;
  border-top-color: #58a6ff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 3rem;
  color: #8b949e;
  text-align: center;
}

.empty-icon {
  font-size: 2.5rem;
  opacity: 0.5;
}

.empty-state p {
  font-size: 1rem;
  font-weight: 500;
}

.empty-state small {
  font-size: 0.8rem;
  color: #484f58;
}

/* =============================================
   LİG GRUBU VE BAŞLIĞI
   ============================================= */
.league-group {
  border-bottom: 1px solid #21262d;
}

.league-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.6rem 1.25rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.league-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.league-icon {
  font-size: 0.85rem;
}

.league-name {
  font-size: 0.8rem;
  font-weight: 600;
  color: #c9d1d9;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

/* =============================================
   MAÇ SATIRI
   ============================================= */
.match-row {
  display: flex;
  align-items: center;
  padding: 0.6rem 1.25rem;
  cursor: pointer;
  transition: background 0.15s;
  border-bottom: 1px solid #21262d10;
}

.match-row:hover {
  background: #1c2129;
}

.match-row:last-child {
  border-bottom: none;
}

/* Saat / durum sütunu */
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

.match-minute {
  font-size: 0.8rem;
  font-weight: 700;
  color: #f85149;
}

.match-clock {
  font-size: 0.8rem;
  color: #8b949e;
  font-weight: 500;
}

.status-badge {
  font-size: 0.7rem;
  font-weight: 600;
  padding: 0.15rem 0.4rem;
  border-radius: 3px;
}

.status-badge.halftime {
  background: #da3633;
  color: #ffffff;
}

.status-badge.finished {
  color: #8b949e;
  background: #21262d;
}

/* Takımlar sütunu */
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

.team-name {
  font-size: 0.85rem;
  font-weight: 400;
  color: #c9d1d9;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.team-name.winner {
  font-weight: 700;
  color: #ffffff;
}

.team-score {
  font-size: 0.85rem;
  font-weight: 700;
  color: #e1e4e8;
  min-width: 18px;
  text-align: center;
}

.team-score.is-live {
  color: #f85149;
}

/* Aksiyon sütunu */
.match-actions-col {
  width: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
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

.fav-btn:hover {
  color: #e3b341;
  transform: scale(1.15);
}

.fav-btn.active {
  color: #e3b341;
  text-shadow: 0 0 8px #e3b34166;
}

/* =============================================
   SAĞ PANEL: PUAN TABLOSU
   ============================================= */
.right-panel {
  background: #0d1117;
  overflow-y: auto;
  max-height: calc(100vh - 112px);
  position: sticky;
  top: 112px;
}

.standings-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 3rem 2rem;
  min-height: 300px;
}

.placeholder-icon {
  font-size: 3rem;
  opacity: 0.3;
  margin-bottom: 1rem;
}

.placeholder-title {
  font-size: 1rem;
  font-weight: 600;
  color: #c9d1d9;
  margin-bottom: 0.5rem;
}

.placeholder-desc {
  font-size: 0.8rem;
  color: #484f58;
  max-width: 220px;
  line-height: 1.5;
}

.standings-panel {
  padding: 0;
}

.standings-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 1.25rem;
  background: #161b22;
  border-bottom: 1px solid #21262d;
}

.standings-league-icon {
  font-size: 1rem;
}

.standings-title {
  font-size: 0.9rem;
  font-weight: 700;
  color: #ffffff;
  margin: 0;
}

.standings-empty {
  padding: 2rem 1.25rem;
  text-align: center;
  color: #484f58;
  font-size: 0.85rem;
}

.standings-empty small {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.75rem;
}

/* =============================================
   PUAN TABLOSU
   ============================================= */
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

.standings-table th {
  padding: 0.6rem 0.4rem;
  text-align: center;
  font-weight: 600;
  color: #8b949e;
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  border-bottom: 1px solid #21262d;
}

.standings-table td {
  padding: 0.55rem 0.4rem;
  text-align: center;
  border-bottom: 1px solid #21262d10;
  color: #c9d1d9;
}

.standings-table tbody tr {
  transition: background 0.15s;
}

.standings-table tbody tr:hover {
  background: #161b22;
}

/* Sıra numarası sütunu */
.col-pos {
  width: 32px;
  font-weight: 600;
  color: #8b949e;
}

/* Takım ismi sütunu */
.col-team {
  text-align: left !important;
  padding-left: 0.6rem !important;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 120px;
}

/* İstatistik sütunları */
.col-stat {
  width: 30px;
  color: #8b949e;
}

/* Puan sütunu */
.col-pts {
  width: 35px;
  font-weight: 700 !important;
  color: #ffffff !important;
}

/* Pozitif / negatif averaj renkleri */
.positive {
  color: #3fb950 !important;
}

.negative {
  color: #f85149 !important;
}

/* Şampiyonlar hattı (sıra renkleri) */
.row-champion {
  border-left: 3px solid #58a6ff;
}

.row-champion .col-pos {
  color: #58a6ff;
}

.row-europa {
  border-left: 3px solid #f0883e;
}

.row-europa .col-pos {
  color: #f0883e;
}

.row-relegation {
  border-left: 3px solid #f85149;
}

.row-relegation .col-pos {
  color: #f85149;
}

/* =============================================
   PUAN TABLOSU LEJAND
   ============================================= */
.standings-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 0.85rem 1.25rem;
  border-top: 1px solid #21262d;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.7rem;
  color: #8b949e;
}

.legend-color {
  width: 10px;
  height: 10px;
  border-radius: 2px;
}

.legend-color.champ {
  background: #58a6ff;
}

.legend-color.europa {
  background: #f0883e;
}

.legend-color.relegation {
  background: #f85149;
}

/* =============================================
   RESPONSIVE TASARIM (Mobil Uyumluluk)
   ============================================= */
@media (max-width: 900px) {
  .main-layout {
    grid-template-columns: 1fr;
  }

  .right-panel {
    position: static;
    max-height: none;
    border-top: 1px solid #21262d;
  }

  .filter-bar {
    flex-direction: column;
    gap: 0.5rem;
    align-items: flex-start;
  }

  .league-dropdown {
    width: 100%;
  }
}

@media (max-width: 600px) {
  .top-header {
    padding: 0 1rem;
  }

  .header-nav {
    display: none;
  }

  .sport-nav {
    padding: 0 0.5rem;
  }

  .sport-tab {
    padding: 0.6rem 0.75rem;
    font-size: 0.78rem;
  }

  .sport-tab-label {
    display: none;
  }

  .match-row {
    padding: 0.5rem 0.75rem;
  }
}
</style>
