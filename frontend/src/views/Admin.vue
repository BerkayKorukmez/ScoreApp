<template>
  <div class="admin-layout">
    <!-- Sidebar -->
    <aside class="admin-sidebar">
      <div class="sidebar-logo">
        <span class="logo-icon">⚡</span>
        <span class="logo-text">Admin Panel</span>
      </div>
      <nav class="sidebar-nav">
        <button
          class="nav-item"
          :class="{ active: activeTab === 'matches' }"
          @click="activeTab = 'matches'"
        >
          <span class="nav-icon">⚽</span>
          <span>Maç Yönetimi</span>
        </button>
        <button
          class="nav-item"
          :class="{ active: activeTab === 'users' }"
          @click="activeTab = 'users'"
        >
          <span class="nav-icon">👤</span>
          <span>Kullanıcılar</span>
        </button>
      </nav>
      <div class="sidebar-footer">
        <RouterLink to="/" class="back-link">← Ana Sayfaya Dön</RouterLink>
        <button class="logout-btn" @click="handleLogout">Çıkış Yap</button>
      </div>
    </aside>

    <!-- Ana içerik -->
    <main class="admin-main">

      <!-- ═══════════════════════ MAÇ YÖNETİMİ ═══════════════════════ -->
      <section v-if="activeTab === 'matches'">
        <div class="section-header">
          <h1>Maç Yönetimi</h1>
          <p class="section-desc">Veritabanındaki maçları gizleyebilir veya yeniden gösterebilirsiniz.</p>
        </div>

        <div class="toolbar">
          <input
            v-model="matchSearch"
            class="search-input"
            placeholder="Takım veya lig ara..."
            @input="onMatchSearch"
          />
          <span class="result-count">{{ matchTotal }} maç</span>
        </div>

        <div v-if="matchesLoading" class="loading-box">
          <div class="spinner"></div>
          <span>Maçlar yükleniyor...</span>
        </div>

        <div v-else-if="matches.length === 0" class="empty-state">
          Kayıtlı maç bulunamadı.
        </div>

        <div v-else class="table-wrapper">
          <table class="admin-table">
            <thead>
              <tr>
                <th>Maç</th>
                <th>Lig</th>
                <th>Spor</th>
                <th>Durum</th>
                <th>Tarih</th>
                <th>Görünürlük</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="match in matches" :key="match.id" :class="{ hidden: match.isHidden }">
                <td>
                  <div class="match-teams">
                    <img v-if="match.homeTeamLogo" :src="match.homeTeamLogo" class="team-logo" @error="$event.target.style.display='none'" />
                    <span>{{ match.homeTeam }}</span>
                    <span class="score-badge">{{ match.homeScore }} - {{ match.awayScore }}</span>
                    <span>{{ match.awayTeam }}</span>
                    <img v-if="match.awayTeamLogo" :src="match.awayTeamLogo" class="team-logo" @error="$event.target.style.display='none'" />
                  </div>
                </td>
                <td>{{ match.league }}</td>
                <td>
                  <span class="sport-badge" :class="sportClass(match.sportType)">
                    {{ sportLabel(match.sportType) }}
                  </span>
                </td>
                <td>
                  <span class="status-badge" :class="statusClass(match.status)">
                    {{ statusLabel(match.status) }}
                  </span>
                </td>
                <td class="date-cell">{{ formatDate(match.startTime) }}</td>
                <td>
                  <button
                    class="visibility-btn"
                    :class="match.isHidden ? 'btn-show' : 'btn-hide'"
                    :disabled="togglingMatch === match.id"
                    @click="toggleMatch(match)"
                  >
                    <span v-if="togglingMatch === match.id">...</span>
                    <span v-else-if="match.isHidden">👁 Göster</span>
                    <span v-else>🚫 Gizle</span>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="matchTotal > matchPageSize" class="pagination">
          <button :disabled="matchPage === 1" @click="matchPage--; loadMatches()">‹ Önceki</button>
          <span>Sayfa {{ matchPage }} / {{ Math.ceil(matchTotal / matchPageSize) }}</span>
          <button :disabled="matchPage >= Math.ceil(matchTotal / matchPageSize)" @click="matchPage++; loadMatches()">Sonraki ›</button>
        </div>
      </section>

      <!-- ═══════════════════════ KULLANICI YÖNETİMİ ═══════════════════════ -->
      <section v-if="activeTab === 'users'">
        <div class="section-header">
          <h1>Kullanıcı Yönetimi</h1>
          <p class="section-desc">Kullanıcıları yönetin, şifrelerini sıfırlayın veya hesapları silin.</p>
        </div>

        <div class="toolbar">
          <input
            v-model="userSearch"
            class="search-input"
            placeholder="İsim veya email ara..."
            @input="onUserSearch"
          />
          <span class="result-count">{{ userTotal }} kullanıcı</span>
        </div>

        <div v-if="usersLoading" class="loading-box">
          <div class="spinner"></div>
          <span>Kullanıcılar yükleniyor...</span>
        </div>

        <div v-else-if="users.length === 0" class="empty-state">
          Kullanıcı bulunamadı.
        </div>

        <div v-else class="table-wrapper">
          <table class="admin-table">
            <thead>
              <tr>
                <th>Kullanıcı</th>
                <th>Email</th>
                <th>Rol</th>
                <th>İşlemler</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="u in users" :key="u.id">
                <td>
                  <div class="user-cell">
                    <div class="user-avatar">{{ (u.userName || '?')[0].toUpperCase() }}</div>
                    <div>
                      <div class="user-name">{{ u.userName }}</div>
                      <div class="user-fullname">{{ u.firstName }} {{ u.lastName }}</div>
                    </div>
                  </div>
                </td>
                <td>{{ u.email }}</td>
                <td>
                  <span class="role-badge" :class="u.isAdmin ? 'admin' : 'user'">
                    {{ u.isAdmin ? '👑 Admin' : '👤 Kullanıcı' }}
                  </span>
                </td>
                <td>
                  <div class="action-buttons">
                    <!-- Şifre sıfırla -->
                    <button class="action-btn btn-password" @click="openPasswordModal(u)">
                      🔑 Şifre
                    </button>
                    <!-- Admin toggle (kendi hesabı hariç) -->
                    <button
                      v-if="u.id !== currentUserId"
                      class="action-btn"
                      :class="u.isAdmin ? 'btn-remove-admin' : 'btn-make-admin'"
                      :disabled="togglingAdmin === u.id"
                      @click="toggleAdmin(u)"
                    >
                      <span v-if="togglingAdmin === u.id">...</span>
                      <span v-else>{{ u.isAdmin ? '👤 Admin Kaldır' : '👑 Admin Yap' }}</span>
                    </button>
                    <!-- Sil (kendi hesabı hariç) -->
                    <button
                      v-if="u.id !== currentUserId"
                      class="action-btn btn-delete"
                      @click="confirmDeleteUser(u)"
                    >
                      🗑 Sil
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="userTotal > userPageSize" class="pagination">
          <button :disabled="userPage === 1" @click="userPage--; loadUsers()">‹ Önceki</button>
          <span>Sayfa {{ userPage }} / {{ Math.ceil(userTotal / userPageSize) }}</span>
          <button :disabled="userPage >= Math.ceil(userTotal / userPageSize)" @click="userPage++; loadUsers()">Sonraki ›</button>
        </div>
      </section>
    </main>

    <!-- ─── Şifre Sıfırlama Modal ─── -->
    <Teleport to="body">
      <div v-if="passwordModal.open" class="modal-overlay" @click.self="closePasswordModal">
        <div class="modal">
          <h2>Şifre Sıfırla</h2>
          <p class="modal-user">{{ passwordModal.user?.userName }} ({{ passwordModal.user?.email }})</p>
          <input
            v-model="passwordModal.newPassword"
            type="password"
            class="modal-input"
            placeholder="Yeni şifre (min. 6 karakter)"
            @keyup.enter="submitPasswordReset"
          />
          <div v-if="passwordModal.error" class="modal-error">{{ passwordModal.error }}</div>
          <div v-if="passwordModal.success" class="modal-success">{{ passwordModal.success }}</div>
          <div class="modal-actions">
            <button class="modal-btn btn-cancel" @click="closePasswordModal">İptal</button>
            <button class="modal-btn btn-confirm" :disabled="passwordModal.loading" @click="submitPasswordReset">
              {{ passwordModal.loading ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ─── Kullanıcı Silme Onay Modal ─── -->
    <Teleport to="body">
      <div v-if="deleteModal.open" class="modal-overlay" @click.self="closeDeleteModal">
        <div class="modal">
          <h2>Kullanıcıyı Sil</h2>
          <p>
            <strong>{{ deleteModal.user?.userName }}</strong> adlı kullanıcıyı silmek istediğinizden emin misiniz?
            Bu işlem geri alınamaz.
          </p>
          <div v-if="deleteModal.error" class="modal-error">{{ deleteModal.error }}</div>
          <div class="modal-actions">
            <button class="modal-btn btn-cancel" @click="closeDeleteModal">İptal</button>
            <button class="modal-btn btn-delete-confirm" :disabled="deleteModal.loading" @click="submitDeleteUser">
              {{ deleteModal.loading ? 'Siliniyor...' : 'Evet, Sil' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import {
  fetchAdminMatches,
  toggleMatchVisibility,
  fetchAdminUsers,
  resetUserPassword,
  deleteUser,
  toggleUserAdmin
} from '../api/adminApi'

const router     = useRouter()
const authStore  = useAuthStore()
const activeTab  = ref('matches')
const currentUserId = computed(() => authStore.user?.id)

// ─── Maçlar ───────────────────────────────────────────────────────────────────
const matches       = ref([])
const matchTotal    = ref(0)
const matchPage     = ref(1)
const matchPageSize = 50
const matchSearch   = ref('')
const matchesLoading = ref(false)
const togglingMatch  = ref(null)

let matchSearchTimeout = null
const onMatchSearch = () => {
  clearTimeout(matchSearchTimeout)
  matchSearchTimeout = setTimeout(() => { matchPage.value = 1; loadMatches() }, 400)
}

const loadMatches = async () => {
  matchesLoading.value = true
  try {
    const data = await fetchAdminMatches(matchPage.value, matchPageSize, matchSearch.value)
    matches.value    = data.data
    matchTotal.value = data.total
  } catch (e) {
    console.error('Maçlar yüklenemedi:', e)
  } finally {
    matchesLoading.value = false
  }
}

const toggleMatch = async (match) => {
  togglingMatch.value = match.id
  try {
    const matchData = {
      homeTeam: match.homeTeam,
      awayTeam: match.awayTeam,
      league: match.league,
      sportType: match.sportType,
      status: match.status,
      startTime: match.startTime
    }
    const res = await toggleMatchVisibility(match.id, matchData)
    match.isHidden = res.isHidden
  } catch (e) {
    console.error('Görünürlük değiştirilemedi:', e)
  } finally {
    togglingMatch.value = null
  }
}

// ─── Kullanıcılar ─────────────────────────────────────────────────────────────
const users       = ref([])
const userTotal   = ref(0)
const userPage    = ref(1)
const userPageSize = 50
const userSearch   = ref('')
const usersLoading = ref(false)
const togglingAdmin = ref(null)

let userSearchTimeout = null
const onUserSearch = () => {
  clearTimeout(userSearchTimeout)
  userSearchTimeout = setTimeout(() => { userPage.value = 1; loadUsers() }, 400)
}

const loadUsers = async () => {
  usersLoading.value = true
  try {
    const data = await fetchAdminUsers(userPage.value, userPageSize, userSearch.value)
    users.value     = data.data
    userTotal.value = data.total
  } catch (e) {
    console.error('Kullanıcılar yüklenemedi:', e)
  } finally {
    usersLoading.value = false
  }
}

const toggleAdmin = async (u) => {
  togglingAdmin.value = u.id
  try {
    const res = await toggleUserAdmin(u.id)
    u.isAdmin = res.isAdmin
  } catch (e) {
    console.error('Admin rolü değiştirilemedi:', e)
  } finally {
    togglingAdmin.value = null
  }
}

// ─── Şifre Modal ──────────────────────────────────────────────────────────────
const passwordModal = ref({ open: false, user: null, newPassword: '', loading: false, error: '', success: '' })

const openPasswordModal = (u) => {
  passwordModal.value = { open: true, user: u, newPassword: '', loading: false, error: '', success: '' }
}
const closePasswordModal = () => { passwordModal.value.open = false }

const submitPasswordReset = async () => {
  if (!passwordModal.value.newPassword || passwordModal.value.newPassword.length < 6) {
    passwordModal.value.error = 'Şifre en az 6 karakter olmalıdır.'
    return
  }
  passwordModal.value.loading = true
  passwordModal.value.error   = ''
  passwordModal.value.success = ''
  try {
    await resetUserPassword(passwordModal.value.user.id, passwordModal.value.newPassword)
    passwordModal.value.success = 'Şifre başarıyla sıfırlandı!'
    passwordModal.value.newPassword = ''
    setTimeout(closePasswordModal, 1500)
  } catch (e) {
    passwordModal.value.error = e.response?.data?.message || 'Şifre sıfırlanamadı.'
  } finally {
    passwordModal.value.loading = false
  }
}

// ─── Silme Modal ──────────────────────────────────────────────────────────────
const deleteModal = ref({ open: false, user: null, loading: false, error: '' })

const confirmDeleteUser = (u) => { deleteModal.value = { open: true, user: u, loading: false, error: '' } }
const closeDeleteModal  = () => { deleteModal.value.open = false }

const submitDeleteUser = async () => {
  deleteModal.value.loading = true
  deleteModal.value.error   = ''
  try {
    await deleteUser(deleteModal.value.user.id)
    users.value = users.value.filter(u => u.id !== deleteModal.value.user.id)
    userTotal.value--
    closeDeleteModal()
  } catch (e) {
    deleteModal.value.error = e.response?.data?.message || 'Kullanıcı silinemedi.'
  } finally {
    deleteModal.value.loading = false
  }
}

// ─── Yardımcı ─────────────────────────────────────────────────────────────────
const sportLabel = (type) => ({ 0: 'Futbol', 1: 'Basketbol', 2: 'Amerikan Futbolu', 3: 'Voleybol', 4: 'Tenis' }[type] ?? '?')
const sportClass = (type) => ({ 0: 'football', 1: 'basketball', 2: 'american-football', 3: 'volleyball', 4: 'tennis' }[type] ?? '')
const statusLabel = (s) => ({ 0: 'Planlandı', 1: 'Canlı', 2: 'Tamamlandı', 3: 'Ertelendi', 4: 'İptal' }[s] ?? '?')
const statusClass = (s) => ({ 0: 'scheduled', 1: 'live', 2: 'finished', 3: 'postponed', 4: 'cancelled' }[s] ?? '')

const formatDate = (dt) => {
  if (!dt) return '-'
  return new Date(dt).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' })
}

const handleLogout = () => { authStore.logout(); router.push('/login') }

onMounted(() => {
  loadMatches()
  loadUsers()
})
</script>

<style scoped>
/* ─── Layout ─────────────────────────────────────────────────────────────── */
.admin-layout {
  display: flex;
  min-height: 100vh;
  background: #0f1117;
  color: #e2e8f0;
  font-family: 'Segoe UI', system-ui, sans-serif;
}

/* ─── Sidebar ─────────────────────────────────────────────────────────────── */
.admin-sidebar {
  width: 240px;
  background: #161b27;
  border-right: 1px solid #2d3748;
  display: flex;
  flex-direction: column;
  position: sticky;
  top: 0;
  height: 100vh;
}

.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 24px 20px;
  border-bottom: 1px solid #2d3748;
  font-size: 1.1rem;
  font-weight: 700;
  color: #a78bfa;
}
.logo-icon { font-size: 1.4rem; }

.sidebar-nav {
  flex: 1;
  padding: 16px 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border-radius: 10px;
  border: none;
  background: transparent;
  color: #94a3b8;
  cursor: pointer;
  font-size: 0.95rem;
  width: 100%;
  text-align: left;
  transition: all 0.15s;
}
.nav-item:hover   { background: #1e2533; color: #e2e8f0; }
.nav-item.active  { background: #2d2060; color: #a78bfa; font-weight: 600; }
.nav-icon { font-size: 1.1rem; }

.sidebar-footer {
  padding: 16px;
  border-top: 1px solid #2d3748;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.back-link  { color: #64748b; font-size: 0.85rem; text-decoration: none; transition: color 0.15s; }
.back-link:hover { color: #a78bfa; }
.logout-btn {
  padding: 8px 0;
  background: #3f1f1f;
  color: #fc8181;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: background 0.15s;
}
.logout-btn:hover { background: #5a2020; }

/* ─── Main ─────────────────────────────────────────────────────────────────── */
.admin-main {
  flex: 1;
  padding: 32px;
  overflow-y: auto;
}

.section-header { margin-bottom: 24px; }
.section-header h1 { font-size: 1.6rem; font-weight: 700; color: #f1f5f9; margin: 0 0 6px; }
.section-desc { color: #64748b; font-size: 0.9rem; margin: 0; }

/* ─── Toolbar ─────────────────────────────────────────────────────────────── */
.toolbar {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 20px;
}
.search-input {
  flex: 1;
  max-width: 380px;
  padding: 10px 16px;
  border-radius: 10px;
  border: 1px solid #2d3748;
  background: #1a2033;
  color: #e2e8f0;
  font-size: 0.9rem;
  outline: none;
  transition: border-color 0.15s;
}
.search-input:focus { border-color: #a78bfa; }
.result-count { color: #64748b; font-size: 0.85rem; }

/* ─── Table ─────────────────────────────────────────────────────────────────── */
.table-wrapper { overflow-x: auto; border-radius: 12px; border: 1px solid #2d3748; }
.admin-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.88rem;
}
.admin-table thead { background: #1a2033; }
.admin-table th {
  padding: 12px 16px;
  text-align: left;
  color: #64748b;
  font-weight: 600;
  font-size: 0.8rem;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  white-space: nowrap;
}
.admin-table tbody tr {
  border-top: 1px solid #1e2533;
  transition: background 0.1s;
}
.admin-table tbody tr:hover { background: #1a2033; }
.admin-table tbody tr.hidden  { opacity: 0.5; }
.admin-table td { padding: 12px 16px; vertical-align: middle; }

/* ─── Match teams ─────────────────────────────────────────────────────────── */
.match-teams {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.9rem;
}
.team-logo { width: 22px; height: 22px; object-fit: contain; }
.score-badge {
  background: #1e2533;
  padding: 2px 8px;
  border-radius: 6px;
  font-weight: 600;
  font-size: 0.8rem;
}

/* ─── Sport / status badges ───────────────────────────────────────────────── */
.sport-badge, .status-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 600;
}
.sport-badge.football   { background: #1c3a1c; color: #68d391; }
.sport-badge.basketball { background: #3a2a1c; color: #f6ad55; }
.sport-badge.volleyball { background: #1c2c3a; color: #63b3ed; }
.sport-badge.tennis     { background: #2a3a1c; color: #c6f135; }

.status-badge.live      { background: #3f1f1f; color: #fc8181; }
.status-badge.finished  { background: #1a2533; color: #94a3b8; }
.status-badge.scheduled { background: #1c2c1c; color: #68d391; }
.status-badge.postponed { background: #3a2a0a; color: #f6e05e; }
.status-badge.cancelled { background: #2d1a1a; color: #fc8181; }

.date-cell { white-space: nowrap; color: #64748b; font-size: 0.82rem; }

/* ─── Visibility buttons ─────────────────────────────────────────────────── */
.visibility-btn {
  padding: 6px 14px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.82rem;
  font-weight: 600;
  transition: all 0.15s;
  white-space: nowrap;
}
.btn-hide { background: #3f1f1f; color: #fc8181; }
.btn-hide:hover { background: #5a2020; }
.btn-show { background: #1c3a1c; color: #68d391; }
.btn-show:hover { background: #1f4a1f; }

/* ─── User cell ──────────────────────────────────────────────────────────── */
.user-cell { display: flex; align-items: center; gap: 12px; }
.user-avatar {
  width: 36px; height: 36px;
  background: #2d2060;
  color: #a78bfa;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-weight: 700;
  font-size: 1rem;
  flex-shrink: 0;
}
.user-name { font-weight: 600; color: #e2e8f0; }
.user-fullname { font-size: 0.78rem; color: #64748b; }

/* ─── Role badge ─────────────────────────────────────────────────────────── */
.role-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 0.78rem;
  font-weight: 600;
}
.role-badge.admin { background: #2d2060; color: #a78bfa; }
.role-badge.user  { background: #1a2033; color: #64748b; }

/* ─── Action buttons ─────────────────────────────────────────────────────── */
.action-buttons { display: flex; gap: 8px; flex-wrap: wrap; }
.action-btn {
  padding: 5px 12px;
  border: none;
  border-radius: 7px;
  cursor: pointer;
  font-size: 0.78rem;
  font-weight: 600;
  transition: all 0.15s;
  white-space: nowrap;
}
.btn-password    { background: #1a2c40; color: #63b3ed; }
.btn-password:hover { background: #1f3650; }
.btn-make-admin  { background: #2d2060; color: #a78bfa; }
.btn-make-admin:hover { background: #3d2880; }
.btn-remove-admin { background: #3a2a0a; color: #f6e05e; }
.btn-remove-admin:hover { background: #4a3a0a; }
.btn-delete      { background: #3f1f1f; color: #fc8181; }
.btn-delete:hover { background: #5a2020; }
.action-btn:disabled { opacity: 0.5; cursor: not-allowed; }

/* ─── Pagination ─────────────────────────────────────────────────────────── */
.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;
  margin-top: 24px;
}
.pagination button {
  padding: 8px 18px;
  background: #1a2033;
  border: 1px solid #2d3748;
  border-radius: 8px;
  color: #e2e8f0;
  cursor: pointer;
  transition: all 0.15s;
}
.pagination button:hover:not(:disabled) { background: #2d3748; }
.pagination button:disabled { opacity: 0.4; cursor: not-allowed; }
.pagination span { color: #64748b; font-size: 0.88rem; }

/* ─── Loading / empty ────────────────────────────────────────────────────── */
.loading-box {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 40px;
  color: #64748b;
}
.spinner {
  width: 24px; height: 24px;
  border: 3px solid #2d3748;
  border-top-color: #a78bfa;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.empty-state {
  padding: 60px;
  text-align: center;
  color: #4a5568;
  font-size: 0.95rem;
}

/* ─── Modal ──────────────────────────────────────────────────────────────── */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.65);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal {
  background: #1a2033;
  border: 1px solid #2d3748;
  border-radius: 16px;
  padding: 28px 32px;
  min-width: 380px;
  max-width: 480px;
  width: 90%;
}
.modal h2 { margin: 0 0 6px; color: #f1f5f9; font-size: 1.2rem; }
.modal-user { color: #64748b; font-size: 0.88rem; margin: 0 0 20px; }
.modal p { color: #94a3b8; font-size: 0.92rem; line-height: 1.6; }

.modal-input {
  width: 100%;
  box-sizing: border-box;
  padding: 10px 14px;
  border-radius: 10px;
  border: 1px solid #2d3748;
  background: #0f1117;
  color: #e2e8f0;
  font-size: 0.95rem;
  outline: none;
  margin-bottom: 12px;
  transition: border-color 0.15s;
}
.modal-input:focus { border-color: #a78bfa; }

.modal-error   { color: #fc8181; font-size: 0.85rem; margin-bottom: 12px; }
.modal-success { color: #68d391; font-size: 0.85rem; margin-bottom: 12px; }

.modal-actions { display: flex; gap: 10px; justify-content: flex-end; margin-top: 8px; }
.modal-btn {
  padding: 9px 20px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.9rem;
  font-weight: 600;
  transition: all 0.15s;
}
.btn-cancel       { background: #1e2533; color: #94a3b8; }
.btn-cancel:hover { background: #2d3748; }
.btn-confirm      { background: #2d2060; color: #a78bfa; }
.btn-confirm:hover { background: #3d2880; }
.btn-confirm:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-delete-confirm      { background: #3f1f1f; color: #fc8181; }
.btn-delete-confirm:hover { background: #5a2020; }
.btn-delete-confirm:disabled { opacity: 0.5; cursor: not-allowed; }
</style>
