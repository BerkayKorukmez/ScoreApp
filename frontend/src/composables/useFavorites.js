import { ref, onMounted } from 'vue'
import {
  fetchFavoriteMatchIds,
  addFavoriteMatch,
  removeFavoriteMatch
} from '../api/matchApi'

/**
 * Favori maç yönetimi composable'ı.
 * Mount olunca favorileri otomatik yükler, ekler/çıkarır.
 */
export function useFavorites() {
  const favoriteMatchIds = ref([])

  /** Kullanıcının favori maç ID listesini yükler (401 = giriş yapılmamış, sessizce geç) */
  const loadFavorites = async () => {
    try {
      favoriteMatchIds.value = await fetchFavoriteMatchIds()
    } catch (error) {
      // 401 → giriş yapılmamış, boş kal
      favoriteMatchIds.value = []
    }
  }

  /** Maç ID'sinin favori olup olmadığını kontrol eder */
  const isFavorite = (matchId) => favoriteMatchIds.value.includes(matchId)

  /**
   * Maçı favorilere ekler ya da favorilerden çıkarır (toggle).
   * @param {string} matchId - Maç ID'si
   */
  const toggleFavorite = async (matchId) => {
    if (!matchId) return

    if (isFavorite(matchId)) {
      try {
        await removeFavoriteMatch(matchId)
        favoriteMatchIds.value = favoriteMatchIds.value.filter((x) => x !== matchId)
      } catch (error) {
        console.error('[useFavorites] Favori kaldırılamadı:', error.response?.status)
      }
    } else {
      try {
        await addFavoriteMatch(matchId)
        favoriteMatchIds.value = [...favoriteMatchIds.value, matchId]
      } catch (error) {
        console.error('[useFavorites] Favoriye eklenemedi:', error.response?.status)
      }
    }
  }

  // Bileşen mount olunca favorileri yükle
  onMounted(loadFavorites)

  return { favoriteMatchIds, isFavorite, toggleFavorite, loadFavorites }
}
