import axios from 'axios'

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

  const response = await axios.get('/match', { params })
  return Array.isArray(response.data) ? response.data : []
}

/**
 * ID'ye göre tek bir maç çeker
 * @param {string} matchId
 */
export const fetchMatchById = async (matchId) => {
  const response = await axios.get(`/match/${matchId}`)
  return response.data
}

/**
 * Kullanıcının favori maç ID listesini çeker
 */
export const fetchFavoriteMatchIds = async () => {
  const response = await axios.get('/favoritematches')
  return Array.isArray(response.data) ? response.data : []
}

/**
 * Bir maçı favorilere ekler
 * @param {string} matchId
 */
export const addFavoriteMatch = async (matchId) => {
  const response = await axios.post('/favoritematches', { matchId })
  return response.data
}

/**
 * Bir maçı favorilerden çıkarır
 * @param {string} matchId
 */
export const removeFavoriteMatch = async (matchId) => {
  const response = await axios.delete(`/favoritematches/${encodeURIComponent(matchId)}`)
  return response.data
}
