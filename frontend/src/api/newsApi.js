import axios from 'axios'

const NEWS_API_KEY = import.meta.env.VITE_NEWS_API_KEY
const BASE_URL = 'https://newsdata.io/api/1/news'

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
 * Başlık veya açıklamada en az bir anahtar kelime geçmeli.
 */
const isSportsRelated = (article) => {
  const text = [article.title, article.description, article.content]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return SPORTS_KEYWORDS.some(kw => text.includes(kw.toLowerCase()))
}

/**
 * Futbol, basketbol ve voleybol haberlerini NewsData.io API'sinden çeker.
 *
 * @param {string} language  - Haber dili (varsayılan: 'tr')
 * @param {string|null} query - Kullanıcının arama sorgusu (opsiyonel)
 * @param {string|null} nextPage - Sonraki sayfa token'ı (opsiyonel)
 */
export const fetchSportsNews = async (language = 'tr', query = null, nextPage = null) => {
  const params = {
    apikey: NEWS_API_KEY,
    category: 'sports',
    language: language,
    // Varsayılan olarak 3 spor dalı filtresi — kullanıcı arama yaparsa üzerine yaz
    q: query && query.trim() ? query.trim() : DEFAULT_SPORTS_QUERY
  }

  if (nextPage) {
    params.page = nextPage
  }

  const response = await axios.get(BASE_URL, { params })
  const data = response.data

  // ── Client-side ikinci filtre: başlıkta/açıklamada ilgili kelime geçmeli ──
  if (data.results && Array.isArray(data.results)) {
    data.results = data.results.filter(article => {
      // Başlığı olmayan haberleri çıkar
      if (!article.title || !article.title.trim()) return false

      // Kullanıcı kendi aramasını yapıyorsa keyword filtresi uygulama
      if (query && query.trim()) return true

      // Varsayılan modda sadece 3 spor dalına ait haberler
      return isSportsRelated(article)
    })
  }

  return data
}
