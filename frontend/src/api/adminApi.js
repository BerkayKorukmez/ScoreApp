import axios from 'axios'

// ─── Maç yönetimi ────────────────────────────────────────────────────────────

export const fetchAdminMatches = async (page = 1, pageSize = 50, search = '') => {
  const params = { page, pageSize }
  if (search) params.search = search
  const response = await axios.get('/admin/matches', { params })
  return response.data
}

export const toggleMatchVisibility = async (matchId, matchData = null) => {
  const body = matchData
    ? {
        homeTeam: matchData.homeTeam,
        awayTeam: matchData.awayTeam,
        league: matchData.league,
        sportType: matchData.sportType,
        status: matchData.status,
        startTime: matchData.startTime
      }
    : undefined
  const response = await axios.patch(
    `/admin/matches/${encodeURIComponent(matchId)}/visibility`,
    body
  )
  return response.data
}

// ─── Kullanıcı yönetimi ───────────────────────────────────────────────────────

export const fetchAdminUsers = async (page = 1, pageSize = 50, search = '') => {
  const params = { page, pageSize }
  if (search) params.search = search
  const response = await axios.get('/admin/users', { params })
  return response.data
}

export const resetUserPassword = async (userId, newPassword) => {
  const response = await axios.post(`/admin/users/${userId}/reset-password`, { newPassword })
  return response.data
}

export const deleteUser = async (userId) => {
  const response = await axios.delete(`/admin/users/${userId}`)
  return response.data
}

export const toggleUserAdmin = async (userId) => {
  const response = await axios.post(`/admin/users/${userId}/toggle-admin`)
  return response.data
}

// ─── Sohbet yasak yönetimi ────────────────────────────────────────────────────

export const fetchChatBans = async () => {
  const response = await axios.get('/admin/chatbans')
  return response.data
}

export const banUserFromChat = async (userId) => {
  const response = await axios.post(`/admin/chatban/${userId}`)
  return response.data
}

export const unbanUserFromChat = async (userId) => {
  const response = await axios.delete(`/admin/chatban/${userId}`)
  return response.data
}
