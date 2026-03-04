import axios from 'axios'

const NEWS_API_KEY = 'pub_2e338f833cec4955b6d03a0882c60f97'
const BASE_URL = 'https://newsdata.io/api/1/latest'

/**
 * Spor haberlerini NewsData.io API'sinden çeker
 * @param {string} language - Haber dili (varsayılan: 'tr')
 * @param {string|null} query - Arama sorgusu (opsiyonel)
 * @param {string|null} nextPage - Sonraki sayfa token'ı (opsiyonel)
 */
export const fetchSportsNews = async (language = 'tr', query = null, nextPage = null) => {
  const params = {
    apikey: NEWS_API_KEY,
    category: 'sports',
    language: language
  }

  if (query) params.q = query
  if (nextPage) params.page = nextPage

  const response = await axios.get(BASE_URL, { params })
  return response.data
}
