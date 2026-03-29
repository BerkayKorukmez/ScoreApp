import axios from 'axios'

// Haberler backend üzerinden (NewsData anahtarı sunucuda; main.js axios baseURL = /api)

// ── Sadece bu 3 spor dalına ait haberler gösterilecek ────────────────────────
const SPORTS_KEYWORDS = [
  // Futbol
  'futbol', 'futbolcu', 'süper lig', 'superlig', 'şampiyonlar ligi', 'şampiyonlar',
  'premier lig', 'la liga', 'bundesliga', 'serie a', 'ligue 1', 'uefa', 'fifa',
  'milli takım', 'penaltı', 'gol', 'kaleci', 'galatasaray', 'fenerbahçe',
  'beşiktaş', 'trabzonspor', 'football', 'soccer', 'champions league',
  // Basketbol
  'basketbol', 'nba', 'euroleague', 'euro league', 'potaya',
  'basketball',
  // Voleybol
  'voleybol', 'efeler', 'sultanlar', 'volleyball', 'voleybolcu'
]

// API'ye gönderilecek arama sorgusu (OR ile birleştirilmiş)
const DEFAULT_SPORTS_QUERY = 'futbol OR basketbol OR voleybol OR football OR basketball OR volleyball'

/**
 * Bir haberin futbol, basketbol veya voleybol ile ilgili olup olmadığını kontrol eder.
 */
const isSportsRelated = (article) => {
  const text = [article.title, article.description, article.content]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return SPORTS_KEYWORDS.some(kw => text.includes(kw.toLowerCase()))
}

/**
 * Futbol, basketbol ve voleybol haberleri (GET /api/news/sports proxy).
 */
export const fetchSportsNews = async (language = 'tr', query = null, nextPage = null) => {
  const params = {
    language,
    q: query && query.trim() ? query.trim() : DEFAULT_SPORTS_QUERY
  }

  if (nextPage) {
    params.page = nextPage
  }

  const response = await axios.get('/news/sports', { params })
  const data = response.data

  if (data.results && Array.isArray(data.results)) {
    const raw = data.results
    data.results = raw.filter((article) => {
      if (!article.title || !article.title.trim()) return false
      if (query && query.trim()) return true
      return isSportsRelated(article)
    })
    // API spor döndü ama kelime filtresi hepsini kesti → ham listeyi göster
    if (data.results.length === 0 && raw.length > 0) {
      data.results = raw.filter((a) => a.title && a.title.trim())
    }
  }

  return data
}
