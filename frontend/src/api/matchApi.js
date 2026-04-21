import http from './http'

/**
 * Belirtilen spor tipine göre maçları API'den çeker
 * @param {string} sportType - 'football' | 'basketball' | 'volleyball'
 * @param {string|null} league - Lig filtresi (opsiyonel)
 */
export const fetchMatches = async (sportType, league = null) => {
  // Backend SportType enum: Football=0, Basketball=1, AmericanFootball=2, Volleyball=3
  const sportTypeMap = {
    football:   0,
    basketball: 1,
    volleyball: 3
  }

  const params = { sportType: sportTypeMap[sportType] }
  if (league) params.league = league

  const response = await http.get('/match', { params })
  return Array.isArray(response.data) ? response.data : []
}

/**
 * Backend DTO'yu frontend standings modeline çevirir (ortak helper)
 */
const mapStandingsDto = (data) =>
  (Array.isArray(data) ? data : []).map(item => ({
    teamId:       item.teamId   || null,
    name:         item.teamName,
    logo:         item.teamLogo || null,
    played:       item.played,
    won:          item.won,
    drawn:        item.drawn,
    lost:         item.lost,
    goalsFor:     item.goalsFor,
    goalsAgainst: item.goalsAgainst,
    goalDiff:     item.goalDifference,
    points:       item.points
  }))

/**
 * Futbol lig puan durumu (API-Sports).
 * @param {number} leagueId - API-Sports lig ID'si (ör: 203 = Süper Lig, 39 = Premier League)
 * @param {number|null} season - Sezon yılı (ör: 2024); verilmezse backend otomatik belirler
 */
export const fetchFootballStandings = async (leagueId, season = null) => {
  if (!leagueId) return []
  const params = { leagueId }
  if (season) params.season = season
  const response = await http.get('/match/standings/football', { params })
  return mapStandingsDto(response.data)
}

/**
 * Basketbol lig puan durumu
 * @param {number} leagueId
 * @param {string} [season] - "2024-2025" formatında; verilmezse backend otomatik belirler
 */
export const fetchBasketballStandings = async (leagueId, season = null) => {
  if (!leagueId) return []
  const params = { leagueId }
  if (season) params.season = season
  const response = await http.get('/match/standings/basketball', { params })
  return mapStandingsDto(response.data)
}

/**
 * Voleybol lig puan durumu
 * @param {number} leagueId
 * @param {number} [season] - Yıl olarak; verilmezse backend otomatik belirler
 */
export const fetchVolleyballStandings = async (leagueId, season = null) => {
  if (!leagueId) return []
  const params = { leagueId }
  if (season) params.season = season
  const response = await http.get('/match/standings/volleyball', { params })
  return mapStandingsDto(response.data)
}

/**
 * Belirtilen tarih ve spor tipine göre geçmiş maçları API'den çeker
 * @param {string} sportType - 'football' | 'basketball' | 'volleyball'
 * @param {string} date - Tarih (yyyy-MM-dd formatında)
 */
export const fetchMatchHistory = async (sportType, date) => {
  const sportTypeMap = {
    football:   0,
    basketball: 1,
    volleyball: 3
  }

  const params = {
    sportType: sportTypeMap[sportType] ?? 0,
    date
  }

  const response = await http.get('/match/history', { params })
  return Array.isArray(response.data) ? response.data : []
}

/**
 * API-Sports üzerinden lig bazlı son maç sonuçlarını çeker.
 * @param {number} leagueId - API-Sports lig ID'si (ör: 203 = Süper Lig)
 * @param {number|null} season - Sezon yılı; verilmezse backend otomatik belirler
 * @param {number} last - Kaç son maç (varsayılan 15)
 * @returns {Array} [{ homeTeam, awayTeam, homeScore, awayScore, date, isPlayed }]
 */
export const fetchFootballResults = async (leagueId, season = null, last = 15) => {
  if (!leagueId) return []
  const params = { leagueId, last }
  if (season) params.season = season
  const response = await http.get('/match/results/football', { params })
  return Array.isArray(response.data) ? response.data : []
}

/**
 * Gol krallığı (API-Sports topscorers).
 * @param {number} leagueId - API-Sports lig ID'si (ör: 203 = Süper Lig)
 * @param {number|null} season - Sezon yılı; verilmezse backend otomatik belirler
 * @returns {Array} [{ name, goals }]
 */
export const fetchGoalKings = async (leagueId, season = null) => {
  if (!leagueId) return []
  const params = { leagueId }
  if (season) params.season = season
  const response = await http.get('/match/goalKings', { params })
  const data = Array.isArray(response.data) ? response.data : []
  return data.map((item) => ({
    playerId: item.playerId ?? null,
    name:     item.name    ?? '',
    photo:    item.photo   ?? null,
    team:     item.team    ?? null,
    teamLogo: item.teamLogo ?? null,
    goals:    typeof item.goals === 'number' ? item.goals : parseInt(item.goals, 10) || 0
  }))
}

/**
 * ID'ye göre tek bir maç çeker
 * @param {string} matchId
 */
export const fetchMatchById = async (matchId) => {
  const response = await http.get(`/match/${matchId}`)
  return response.data
}

/**
 * Kullanıcının favori maç ID listesini çeker
 */
export const fetchFavoriteMatchIds = async () => {
  const response = await http.get('/favoritematches')
  return Array.isArray(response.data) ? response.data : []
}

/**
 * Bir maçı favorilere ekler
 * @param {string} matchId
 */
export const addFavoriteMatch = async (matchId) => {
  const response = await http.post('/favoritematches', { matchId })
  return response.data
}

/**
 * Bir maçı favorilerden çıkarır
 * @param {string} matchId
 */
export const removeFavoriteMatch = async (matchId) => {
  const response = await http.delete(`/favoritematches/${encodeURIComponent(matchId)}`)
  return response.data
}
