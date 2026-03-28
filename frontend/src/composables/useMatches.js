import { ref, onMounted, watch } from 'vue'
import { fetchMatches as apiFetchMatches } from '../api/matchApi'
import { useWebSocket } from './useWebSocket'

/**
 * Maç listesi yönetimi composable'ı.
 * Sadece veri çekme ve WebSocket canlı güncelleme işleri burada.
 * Filtreleme/gruplama → useLeagues
 *
 * @param {Ref<string>} selectedSportRef - 'football' | 'basketball' | 'volleyball'
 */
export function useMatches(selectedSportRef) {
  const matches   = ref([])
  const isLoading = ref(false)
  const { connect } = useWebSocket()

  // ── API çekme ──────────────────────────────────────────────────────────
  const fetchInitialMatches = async () => {
    isLoading.value = true
    try {
      matches.value = await apiFetchMatches(selectedSportRef.value)
      console.log(`[useMatches] ${matches.value.length} maç yüklendi (${selectedSportRef.value})`)
    } catch (error) {
      console.error('[useMatches] Maçlar yüklenemedi:', error.response?.status, error.message)
      matches.value = []
    } finally {
      isLoading.value = false
    }
  }

  // ── WebSocket canlı güncellemeler ──────────────────────────────────────
  const setupLiveUpdates = () => {
    connect({
      AllMatches: (allMatches) => {
        if (!Array.isArray(allMatches) || allMatches.length === 0) return
        allMatches.forEach((m) => {
          if (!m || m.id == null) return
          const index = matches.value.findIndex((x) => x.id === m.id)
          if (index !== -1) matches.value.splice(index, 1, m)
          else matches.value.push(m)
        })
      },
      MatchUpdated: (m) => {
        if (!m || m.id == null) return
        const index = matches.value.findIndex((x) => x.id === m.id)
        if (index !== -1) matches.value.splice(index, 1, m)
        else matches.value.push(m)
      },
      MatchRemoved: (matchId) => {
        const id = typeof matchId === 'object' && matchId?.id != null ? matchId.id : matchId
        if (id == null) return
        matches.value = matches.value.filter((m) => String(m.id) !== String(id))
      },
      // Admin görünürlük değişikliği — listeyi yeniden çek (merge hatası önlenir)
      MatchVisibilityChanged: () => {
        fetchInitialMatches()
      }
    })
  }

  // ── Yaşam döngüsü ───────────────────────────────────────────────────────
  watch(selectedSportRef, () => { fetchInitialMatches() })

  onMounted(() => {
    setupLiveUpdates()
    fetchInitialMatches()
  })

  return { matches, isLoading, fetchInitialMatches }
}
