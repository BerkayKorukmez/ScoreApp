import http from './http'

/**
 * Belirtilen spor tipine göre maçları API'den çeker
 * @param {string} sportType - 'football' | 'basketball' | 'volleyball' | 'tennis'
 * @param {string|null} league - Lig filtresi (opsiyonel)
 */
export const fetchMatches = async (sportType, league = null) => {
  // Backend SportType enum: Football=0, Basketball=1, AmericanFootball=2, Volleyball=3, Tennis=4
  const sportTypeMap = {
    football:   0,
    basketball: 1,
    volleyball: 3,
    tennis:     4
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
 * Futbol lig puan durumu.
 * collectApiKey varsa CollectAPI (yerli ligler), yoksa leagueId ile API-Sports (turnuvalar).
 * @param {string|null} collectApiKey - CollectAPI lig key'i (ör: 'super-lig')
 * @param {number|null} leagueId     - API-Sports lig ID'si (ör: 2 = UCL)
 */
export const fetchFootballStandings = async (collectApiKey, leagueId = null) => {
  if (collectApiKey) {
    const response = await http.get('/match/standings/football', { params: { collectApiKey } })
    return mapStandingsDto(response.data)
  }
  if (leagueId) {
    const response = await http.get('/match/standings/football', { params: { leagueId } })
    return mapStandingsDto(response.data)
  }
  return []
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
 * CollectAPI üzerinden lig bazlı son hafta maç sonuçlarını çeker.
 * @param {string} collectApiKey - CollectAPI lig key'i (ör: 'super-lig')
 * @returns {Array} [{ homeTeam, awayTeam, homeScore, awayScore, date, isPlayed }]
 */
export const fetchFootballResults = async (collectApiKey, date = null) => {
  if (!collectApiKey) return []
  const params = { collectApiKey }
  if (date) params.date = date
  const response = await http.get('/match/results/football', { params })
  return Array.isArray(response.data) ? response.data : []
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
