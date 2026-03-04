<template>
  <div class="detail-page">

    <!-- ÜST HEADER ÇUBUĞU -->
    <header class="top-header">
      <div class="header-left">
        <button class="btn-back" @click="$router.go(-1)">
          <svg class="back-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          <span>Geri</span>
        </button>
      </div>
      <div class="header-center">
        <span class="brand-icon">⚽</span>
        <span class="brand-text">SkorTakip</span>
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

    <!-- YÜKLENİYOR DURUMU -->
    <div v-if="isLoading" class="state-container">
      <div class="spinner"></div>
      <span class="state-text">Maç yükleniyor...</span>
    </div>

    <!-- HATA DURUMU -->
    <div v-else-if="errorMessage || !match" class="state-container">
      <span class="state-icon">⚠️</span>
      <span class="state-text">{{ errorMessage || 'Maç bulunamadı.' }}</span>
      <button class="btn-go-home" @click="$router.push('/')">Ana Sayfaya Dön</button>
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
          <div class="team-block">
            <div class="team-emblem">
              <span class="emblem-letter">{{ getTeamInitial(match.homeTeam) }}</span>
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
          <div class="team-block">
            <div class="team-emblem">
              <span class="emblem-letter">{{ getTeamInitial(match.awayTeam) }}</span>
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
              :class="['tab-item', { active: activeTab === 'info' }]"
              @click="activeTab = 'info'"
            >
              ℹ️ Bilgiler
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

            <!-- Gerçek istatistikler -->
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
        </div>
      </div>

    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { fetchMatchById } from '../api/matchApi'
import * as signalR from '@microsoft/signalr'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

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

// API'den gelen gerçek istatistikleri göster
const matchStats = computed(() => {
  const s = match.value?.statistics
  if (!s) return []

  const stats = []

  // Topa Sahiplik
  if (s.homePossession || s.awayPossession) {
    stats.push({ label: 'Topa Sahiplik', home: s.homePossession || '0%', away: s.awayPossession || '0%' })
  }

  // Toplam Şut
  if (s.homeShotsTotal !== undefined || s.awayShotsTotal !== undefined) {
    stats.push({ label: 'Toplam Şut', home: s.homeShotsTotal ?? 0, away: s.awayShotsTotal ?? 0 })
  }

  // İsabetli Şut
  if (s.homeShotsOnGoal !== undefined || s.awayShotsOnGoal !== undefined) {
    stats.push({ label: 'İsabetli Şut', home: s.homeShotsOnGoal ?? 0, away: s.awayShotsOnGoal ?? 0 })
  }

  // Engellenen Şut
  if (s.homeBlockedShots !== undefined || s.awayBlockedShots !== undefined) {
    stats.push({ label: 'Engellenen Şut', home: s.homeBlockedShots ?? 0, away: s.awayBlockedShots ?? 0 })
  }

  // Korner
  if (s.homeCorners !== undefined || s.awayCorners !== undefined) {
    stats.push({ label: 'Korner', home: s.homeCorners ?? 0, away: s.awayCorners ?? 0 })
  }

  // Ofsayt
  if (s.homeOffsides !== undefined || s.awayOffsides !== undefined) {
    stats.push({ label: 'Ofsayt', home: s.homeOffsides ?? 0, away: s.awayOffsides ?? 0 })
  }

  // Faul
  if (s.homeFouls !== undefined || s.awayFouls !== undefined) {
    stats.push({ label: 'Faul', home: s.homeFouls ?? 0, away: s.awayFouls ?? 0 })
  }

  // Sarı Kart
  if (s.homeYellowCards !== undefined || s.awayYellowCards !== undefined) {
    stats.push({ label: 'Sarı Kart', home: s.homeYellowCards ?? 0, away: s.awayYellowCards ?? 0 })
  }

  // Kırmızı Kart
  if (s.homeRedCards !== undefined || s.awayRedCards !== undefined) {
    stats.push({ label: 'Kırmızı Kart', home: s.homeRedCards ?? 0, away: s.awayRedCards ?? 0 })
  }

  // Kaleci Kurtarışı
  if (s.homeSaves !== undefined || s.awaySaves !== undefined) {
    stats.push({ label: 'Kaleci Kurtarışı', home: s.homeSaves ?? 0, away: s.awaySaves ?? 0 })
  }

  // Toplam Pas
  if (s.homeTotalPasses !== undefined || s.awayTotalPasses !== undefined) {
    stats.push({ label: 'Toplam Pas', home: s.homeTotalPasses ?? 0, away: s.awayTotalPasses ?? 0 })
  }

  // Pas İsabeti
  if (s.homePassAccuracy || s.awayPassAccuracy) {
    stats.push({ label: 'Pas İsabeti', home: s.homePassAccuracy || '0%', away: s.awayPassAccuracy || '0%' })
  }

  return stats
})

/* =============================================
   YARDIMCI FONKSİYONLAR
   ============================================= */

// Takım adının ilk harfini al (amblem yerine)
const getTeamInitial = (teamName) => {
  if (!teamName) return '?'
  return teamName.charAt(0).toUpperCase()
}

// Oturumu kapat
const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

// Tarih ve saati biçimlendir
const formatDateTime = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return date.toLocaleDateString('tr-TR', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Spor tipi etiketini döndür
const getSportLabel = (sportType) => {
  const labels = { 0: '⚽ Futbol', 1: '🏀 Basketbol', 2: '🏈 Amerikan Futbolu', 3: '🏐 Voleybol', 4: '🎾 Tenis' }
  return labels[sportType] || 'Bilinmiyor'
}

// İstatistik çubuğu yüzdesini hesapla
const getBarPercent = (val, other) => {
  const v = parseFloat(val) || 0
  const o = parseFloat(other) || 0
  if (v + o === 0) return 50
  return Math.round((v / (v + o)) * 100)
}

/* =============================================
   WEBSOCKET BAĞLANTISI (Canlı skor güncellemesi)
   ============================================= */
let wsConnection = null

const setupWebSocket = () => {
  wsConnection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/matchhub')
    .withAutomaticReconnect()
    .build()

  // Tekil maç güncellemesi — skor, dakika, durum canlı güncellenir
  wsConnection.on('MatchUpdated', (updatedMatch) => {
    if (match.value && updatedMatch.id === match.value.id) {
      match.value.homeScore = updatedMatch.homeScore
      match.value.awayScore = updatedMatch.awayScore
      match.value.minute = updatedMatch.minute
      match.value.status = updatedMatch.status
      console.log(`Canlı güncelleme: ${updatedMatch.homeTeam} ${updatedMatch.homeScore}-${updatedMatch.awayScore} ${updatedMatch.awayTeam}`)
    }
  })

  // Cache'ten gelen tüm maçlar
  wsConnection.on('AllMatches', (allMatches) => {
    if (!match.value || !Array.isArray(allMatches)) return
    const updated = allMatches.find(m => m.id === match.value.id)
    if (updated) {
      match.value.homeScore = updated.homeScore
      match.value.awayScore = updated.awayScore
      match.value.minute = updated.minute
      match.value.status = updated.status
    }
  })

  wsConnection.start()
    .then(() => console.log('Maç detay - WebSocket bağlantısı kuruldu ✓'))
    .catch(err => console.error('WebSocket bağlantı hatası:', err))
}

const closeWebSocket = () => {
  if (wsConnection) {
    wsConnection.stop()
    wsConnection = null
  }
}

/* =============================================
   YAŞAM DÖNGÜSÜ
   ============================================= */
onMounted(async () => {
  const matchId = route.params.id

  // WebSocket bağlantısını kur (canlı skor güncellemeleri için)
  setupWebSocket()

  try {
    isStatsLoading.value = true
    match.value = await fetchMatchById(matchId)
    console.log('Maç verisi:', match.value)

    // İstatistik verisi gelmişse loading'i kapat
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

onUnmounted(() => {
  closeWebSocket()
})
</script>

<style scoped>
/* =============================================
   GENEL KONTEYNER
   ============================================= */
.detail-page {
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

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-width: 180px;
}

.header-right {
  justify-content: flex-end;
}

.header-center {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.brand-icon {
  font-size: 1.2rem;
}

.brand-text {
  font-size: 1rem;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: -0.3px;
}

.btn-back {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  background: transparent;
  border: 1px solid #30363d;
  color: #c9d1d9;
  padding: 0.35rem 0.75rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-back:hover {
  background: #21262d;
  border-color: #484f58;
}

.back-icon {
  width: 16px;
  height: 16px;
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

@keyframes spin {
  to { transform: rotate(360deg); }
}

.state-icon {
  font-size: 2.5rem;
  opacity: 0.5;
}

.state-text {
  font-size: 0.95rem;
  color: #8b949e;
}

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

.btn-go-home:hover {
  background: #30363d;
}

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

.league-badge-icon {
  font-size: 0.85rem;
}

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

/* Takım bloğu */
.team-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  min-width: 0;
}

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
}

.emblem-letter {
  font-size: 1.8rem;
  font-weight: 700;
  color: #c9d1d9;
}

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

/* Skor paneli */
.score-center {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.6rem;
  flex-shrink: 0;
}

.score-digits {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.digit {
  font-size: 3rem;
  font-weight: 800;
  color: #e1e4e8;
  line-height: 1;
  min-width: 45px;
  text-align: center;
}

.digit.winner {
  color: #ffffff;
}

.score-separator {
  font-size: 2rem;
  font-weight: 300;
  color: #484f58;
}

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

.match-status.halftime {
  background: #d2992222;
  color: #d29922;
}

.match-status.finished {
  background: #21262d;
  color: #8b949e;
}

.match-status.upcoming {
  background: #58a6ff15;
  color: #58a6ff;
}

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

.tab-item:hover {
  color: #c9d1d9;
  background: #161b22;
}

.tab-item.active {
  color: #58a6ff;
  border-bottom-color: #58a6ff;
}

.tab-content {
  padding: 1rem 1.25rem;
}

/* =============================================
   İSTATİSTİK YÜKLENİYOR / BOŞ DURUMLARI
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

.stats-empty-icon {
  font-size: 2rem;
  opacity: 0.35;
}

.stats-empty p {
  font-size: 0.9rem;
  font-weight: 500;
  color: #8b949e;
  margin: 0;
}

.stats-empty small {
  font-size: 0.78rem;
  color: #484f58;
  max-width: 280px;
  line-height: 1.5;
}

/* =============================================
   İSTATİSTİKLER
   ============================================= */
.stat-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.7rem 0;
  border-bottom: 1px solid #21262d15;
}

.stat-row:last-child {
  border-bottom: none;
}

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

.stat-bar-wrapper {
  display: flex;
  gap: 4px;
  width: 100%;
}

.stat-bar {
  flex: 1;
  height: 4px;
  background: #21262d;
  border-radius: 2px;
  overflow: hidden;
}

.stat-bar:first-child {
  direction: rtl;
}

.stat-bar-fill {
  height: 100%;
  border-radius: 2px;
  transition: width 0.5s ease;
}

.home-fill {
  background: #58a6ff;
}

.away-fill {
  background: #f0883e;
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

.info-item:nth-child(odd) {
  border-right: 1px solid #21262d15;
  padding-right: 1rem;
}

.info-item:nth-child(even) {
  padding-left: 1rem;
}

.info-key {
  font-size: 0.7rem;
  font-weight: 500;
  color: #484f58;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-value {
  font-size: 0.85rem;
  font-weight: 600;
  color: #e1e4e8;
}

.text-live {
  color: #f85149;
}

.text-halftime {
  color: #d29922;
}

/* =============================================
   MOBİL UYUMLULUK
   ============================================= */
@media (max-width: 700px) {
  .scoreboard {
    gap: 1rem;
  }

  .digit {
    font-size: 2.2rem;
    min-width: 32px;
  }

  .team-emblem {
    width: 56px;
    height: 56px;
  }

  .emblem-letter {
    font-size: 1.4rem;
  }

  .team-label {
    font-size: 0.8rem;
    max-width: 100px;
  }

  .content-area {
    padding: 1rem;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }

  .info-item:nth-child(odd) {
    border-right: none;
    padding-right: 0.5rem;
  }

  .info-item:nth-child(even) {
    padding-left: 0.5rem;
  }
}

@media (max-width: 500px) {
  .top-header {
    padding: 0 0.75rem;
  }

  .header-left,
  .header-right {
    min-width: auto;
  }

  .user-info {
    display: none;
  }

  .header-center .brand-text {
    display: none;
  }

  .score-section {
    padding: 1.5rem 1rem 2rem;
  }

  .scoreboard {
    gap: 0.75rem;
  }

  .digit {
    font-size: 1.8rem;
  }

  .team-emblem {
    width: 48px;
    height: 48px;
  }

  .emblem-letter {
    font-size: 1.2rem;
  }
}
</style>
