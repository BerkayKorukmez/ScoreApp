import http from './http'

/**
 * Takım veya lig arar.
 * @param {string} query   - Arama metni
 * @param {string} sport   - 'football' | 'basketball' | 'volleyball'
 * @param {string} kind    - 'team' | 'league'
 */
export const searchFixture = async (query, sport = 'football', kind = 'team') => {
  const response = await http.get('/fixture/search', { params: { query, sport, kind } })
  return Array.isArray(response.data) ? response.data : []
}

/**
 * Takıma göre sezon fikstürü çeker.
 * @param {number} teamId
 * @param {string} sport
 * @param {number|null} season
 */
export const fetchFixturesByTeam = async (teamId, sport = 'football', season = null) => {
  const params = { teamId, sport }
  if (season) params.season = season
  const response = await http.get('/fixture/team', { params })
  return Array.isArray(response.data) ? response.data : []
}

/**
 * Lige göre sezon fikstürü çeker.
 * @param {number} leagueId
 * @param {string} sport
 * @param {number|null} season
 */
export const fetchFixturesByLeague = async (leagueId, sport = 'football', season = null) => {
  const params = { leagueId, sport }
  if (season) params.season = season
  const response = await http.get('/fixture/league', { params })
  return Array.isArray(response.data) ? response.data : []
}
