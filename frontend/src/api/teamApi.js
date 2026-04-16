import http from './http'

/**
 * Kulüp profili: temel bilgi + stadyum + kadro
 * @param {number} teamId - API-Sports takım ID
 */
export const fetchTeamProfile = async (teamId) => {
  const response = await http.get(`/team/${teamId}`)
  return response.data
}

/**
 * Oyuncu profili: biyografi + sezon istatistikleri
 * @param {number} playerId - API-Sports oyuncu ID
 * @param {number|null} season - Sezon yılı; verilmezse backend otomatik belirler
 */
export const fetchPlayerProfile = async (playerId, season = null) => {
  const params = season ? { season } : {}
  const response = await http.get(`/player/${playerId}`, { params })
  return response.data
}
