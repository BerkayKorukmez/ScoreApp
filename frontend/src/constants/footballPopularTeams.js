/**
 * API-Football v3 takım ID'leri (api-sports.io).
 * Logo: https://media.api-sports.io/football/teams/{id}.png
 * ID'ler dashboard.api-football.com → Soccer → Ids → Teams ile doğrulanmalı.
 */
export const footballTeamLogoUrl = (teamId) =>
  `https://media.api-sports.io/football/teams/${teamId}.png`

/**
 * Fikstür "popüler takımlar" kısayolları — isim + id uyumu API listesiyle eşleşmeli.
 */
export const FOOTBALL_POPULAR_QUICK_TEAMS = [
  { id: 645, name: 'Galatasaray', sport: 'football' },
  { id: 611, name: 'Fenerbahçe', sport: 'football' },
  { id: 549, name: 'Beşiktaş', sport: 'football' },
  { id: 998, name: 'Trabzonspor', sport: 'football' },
  { id: 40, name: 'Liverpool', sport: 'football' },
  { id: 50, name: 'Manchester City', sport: 'football' },
  { id: 529, name: 'Barcelona', sport: 'football' },
  { id: 541, name: 'Real Madrid', sport: 'football' }
]
