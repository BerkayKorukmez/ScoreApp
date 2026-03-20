import { ref, watch } from 'vue'
import {
  fetchFootballStandings,
  fetchBasketballStandings,
  fetchVolleyballStandings
} from '../api/matchApi'
import {
  FOOTBALL_LEAGUE_IDS,
  BASKETBALL_LEAGUE_IDS,
  VOLLEYBALL_LEAGUE_IDS
} from '../constants/sports'

/**
 * Puan durumu yönetimi composable'ı.
 * Seçilen lig ve sport tipine göre puan tablosunu otomatik yükler.
 *
 * @param {Ref<string>}        selectedSportRef  - 'football' | 'basketball' | 'volleyball'
 * @param {Ref<object|null>}   leagueInfoRef     - { name, leagueId, country, ... } veya null
 */
export function useStandings(selectedSportRef, leagueInfoRef) {
  const leagueStandings    = ref([])
  const isStandingsLoading = ref(false)

  const load = async () => {
    const leagueInfo = leagueInfoRef.value
    const sport      = selectedSportRef.value

    leagueStandings.value    = []
    isStandingsLoading.value = false

    if (!leagueInfo) return

    // Sport tipine göre fallback haritası ve fetch fonksiyonu
    let fallbackMap, fetchFn
    if (sport === 'football') {
      fallbackMap = FOOTBALL_LEAGUE_IDS
      fetchFn     = fetchFootballStandings
    } else if (sport === 'basketball') {
      fallbackMap = BASKETBALL_LEAGUE_IDS
      fetchFn     = fetchBasketballStandings
    } else if (sport === 'volleyball') {
      fallbackMap = VOLLEYBALL_LEAGUE_IDS
      fetchFn     = fetchVolleyballStandings
    } else {
      return // Tenis için puan tablosu yok
    }

    // Futbol: CollectAPI key varsa CollectAPI, yoksa leagueId ile API-Sports (UCL, UEL vb.)
    if (sport === 'football') {
      const collectApiKey = leagueInfo.collectApiKey ?? null
      const leagueId      = leagueInfo.leagueId ?? FOOTBALL_LEAGUE_IDS[leagueInfo.name] ?? null

      if (!collectApiKey && !leagueId) {
        console.warn('[useStandings] Bu lig için ne CollectAPI key ne leagueId var:', leagueInfo.name)
        return
      }

      const source = collectApiKey ? `CollectAPI (key=${collectApiKey})` : `API-Sports (id=${leagueId})`
      console.log(`[useStandings] Futbol puan durumu yükleniyor: ${leagueInfo.name} → ${source}`)
      isStandingsLoading.value = true
      try {
        leagueStandings.value = await fetchFootballStandings(collectApiKey, leagueId)
      } catch (error) {
        console.error('[useStandings] Puan durumu yüklenemedi:', error.response?.status, error.message)
        leagueStandings.value = []
      } finally {
        isStandingsLoading.value = false
      }
      return
    }

    // Basketbol / Voleybol: leagueId ile
    const leagueId = leagueInfo.leagueId ?? fallbackMap[leagueInfo.name] ?? null
    if (!leagueId) {
      console.warn('[useStandings] LeagueId bulunamadı:', leagueInfo.name)
      return
    }

    console.log(`[useStandings] Yükleniyor: ${leagueInfo.name} (id=${leagueId}, sport=${sport})`)
    isStandingsLoading.value = true
    try {
      leagueStandings.value = await fetchFn(leagueId)
    } catch (error) {
      console.error('[useStandings] Puan durumu yüklenemedi:', error.response?.status, error.message)
      leagueStandings.value = []
    } finally {
      isStandingsLoading.value = false
    }
  }

  // Lig veya spor değişince otomatik yükle
  watch([leagueInfoRef, selectedSportRef], load, { immediate: true })

  return { leagueStandings, isStandingsLoading }
}
