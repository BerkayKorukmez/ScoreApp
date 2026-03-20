import { ref, computed, watch } from 'vue'
import { SPORTS, POPULAR_LEAGUES, POPULAR_STANDINGS_LEAGUES } from '../constants/sports'

// Ülke sıralama önceliği (en öne gelenler)
const PRIORITY_COUNTRIES = ['Turkey', 'England', 'Spain', 'Italy', 'Germany', 'France', 'World']

/**
 * Lig listeleme, filtreleme ve gruplama composable'ı.
 *
 * @param {Ref<Match[]>}    matchesRef         - Tüm maç listesi (reaktif)
 * @param {Ref<string>}     selectedSportRef   - Seçili spor ('football' | 'basketball' | 'volleyball')
 * @param {Ref<string>}     activeFilterRef    - Aktif filtre ('all' | 'live' | 'finished' | 'favorites')
 * @param {Ref<string[]>}   favoriteMatchIdsRef - Favori maç ID listesi
 */
export function useLeagues(matchesRef, selectedSportRef, activeFilterRef, favoriteMatchIdsRef) {

  /** Seçili lig anahtarı — bu composable tarafından yönetilir */
  const selectedLeagueKey = ref(null)

  // Spor değişince lig seçimini sıfırla
  watch(selectedSportRef, () => {
    selectedLeagueKey.value = null
  })

  // ── Yardımcılar ────────────────────────────────────────────────────────
  const getMatchLeagueKey = (match) =>
    match.leagueCountry
      ? `${match.leagueCountry}::${match.league}`
      : `::${match.league}`

  // ── Lig listesi (dropdown için) ────────────────────────────────────────
  const leaguesByCountry = computed(() => {
    const sportData = SPORTS.find((s) => s.id === selectedSportRef.value)
    if (!sportData) return []

    // Seçili sporun maçlarından benzersiz ligleri topla
    const leagueMap = new Map()
    matchesRef.value
      .filter((m) => m.sportType === sportData.sportType)
      .forEach((m) => {
        if (!m.league) return
        const key = getMatchLeagueKey(m)
        if (!leagueMap.has(key)) {
          leagueMap.set(key, {
            key,
            name:     m.league,
            country:  m.leagueCountry || 'Diğer',
            leagueId: m.externalLeagueId ?? null,
            flag:     m.leagueFlag     ?? null
          })
        }
      })

    // Ülkelere göre grupla
    const countryGroups = {}
    for (const league of leagueMap.values()) {
      if (!countryGroups[league.country]) countryGroups[league.country] = []
      countryGroups[league.country].push(league)
    }

    // Popüler ligleri merge et — bugün maçı olmasa bile dropdown'da göster
    const popularList = POPULAR_STANDINGS_LEAGUES[selectedSportRef.value] ?? []
    popularList.forEach((pop) => {
      const key = `${pop.country}::${pop.name}`
      if (!leagueMap.has(key)) {
        leagueMap.set(key, {
          key,
          name:          pop.name,
          country:       pop.country,
          leagueId:      pop.leagueId,
          flag:          pop.flag,
          collectApiKey: pop.collectApiKey ?? null
        })
        if (!countryGroups[pop.country]) countryGroups[pop.country] = []
        countryGroups[pop.country].push(leagueMap.get(key))
      } else {
        // Lig maç verisinden zaten eklendiyse eksik alanları tamamla
        const existing = leagueMap.get(key)
        if (!existing.collectApiKey && pop.collectApiKey) existing.collectApiKey = pop.collectApiKey
        if (!existing.leagueId    && pop.leagueId)    existing.leagueId    = pop.leagueId
        if (!existing.flag        && pop.flag)        existing.flag        = pop.flag
      }
    })

    // Her ülkenin liglerini popüler ligler önce gelecek şekilde sırala
    for (const country of Object.keys(countryGroups)) {
      countryGroups[country].sort((a, b) => {
        const ia = POPULAR_LEAGUES.indexOf(a.name)
        const ib = POPULAR_LEAGUES.indexOf(b.name)
        if (ia !== -1 && ib !== -1) return ia - ib
        if (ia !== -1) return -1
        if (ib !== -1) return 1
        return a.name.localeCompare(b.name, 'tr-TR')
      })
    }

    // Ülkeleri öncelik listesine göre sırala
    const sortedCountries = Object.keys(countryGroups).sort((a, b) => {
      const ia = PRIORITY_COUNTRIES.indexOf(a)
      const ib = PRIORITY_COUNTRIES.indexOf(b)
      if (ia !== -1 && ib !== -1) return ia - ib
      if (ia !== -1) return -1
      if (ib !== -1) return 1
      return a.localeCompare(b, 'tr-TR')
    })

    return sortedCountries.map((country) => ({
      country,
      leagues: countryGroups[country]
    }))
  })

  // ── Seçili lig bilgisi ─────────────────────────────────────────────────
  const selectedLeagueInfo = computed(() => {
    if (!selectedLeagueKey.value) return null
    for (const group of leaguesByCountry.value) {
      const found = group.leagues.find((l) => l.key === selectedLeagueKey.value)
      if (found) return found
    }
    return null
  })

  // ── Filtreleme ve gruplama ─────────────────────────────────────────────
  const displayedMatches = computed(() => {
    const sportData = SPORTS.find((s) => s.id === selectedSportRef.value)

    let filtered = sportData
      ? matchesRef.value.filter((m) => m.sportType === sportData.sportType)
      : matchesRef.value

    if (selectedLeagueKey.value) {
      filtered = filtered.filter((m) => getMatchLeagueKey(m) === selectedLeagueKey.value)
    }

    if (activeFilterRef.value === 'live') {
      filtered = filtered.filter((m) => m.status === 1)
    } else if (activeFilterRef.value === 'finished') {
      filtered = filtered.filter((m) => m.status === 3)
    } else if (activeFilterRef.value === 'favorites') {
      filtered = filtered.filter((m) => favoriteMatchIdsRef.value.includes(m.id))
    }

    // Canlı maçlar üste, sonra başlangıç zamanına göre sırala
    return [...filtered].sort((a, b) => {
      if (a.status === 1 && b.status !== 1) return -1
      if (a.status !== 1 && b.status === 1) return 1
      return new Date(b.startTime) - new Date(a.startTime)
    })
  })

  const groupedMatches = computed(() => {
    const groups = {}
    displayedMatches.value.forEach((match) => {
      const key = getMatchLeagueKey(match)
      if (!groups[key]) groups[key] = []
      groups[key].push(match)
    })
    return groups
  })

  // ── Sayaçlar ───────────────────────────────────────────────────────────
  const liveMatchCount = computed(() => {
    const sportData = SPORTS.find((s) => s.id === selectedSportRef.value)
    if (!sportData) return 0
    return matchesRef.value.filter(
      (m) => m.sportType === sportData.sportType && m.status === 1
    ).length
  })

  /** Her spor için toplam maç sayısı: { football: 12, basketball: 5, ... } */
  const sportMatchCounts = computed(() => {
    const counts = {}
    SPORTS.forEach((sport) => {
      counts[sport.id] = matchesRef.value.filter((m) => m.sportType === sport.sportType).length
    })
    return counts
  })

  return {
    selectedLeagueKey,   // ← dışarıdan set edilebilir (v-model gibi)
    leaguesByCountry,
    selectedLeagueInfo,
    groupedMatches,
    displayedMatches,
    liveMatchCount,
    sportMatchCounts,
    getMatchLeagueKey
  }
}
