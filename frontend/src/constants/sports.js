/**
 * Spor sabitleri — backend SportType enum değerleriyle eşleşir
 */

// Desteklenen spor tipleri
export const SPORTS = [
  { id: 'football',   icon: '⚽', label: 'Futbol',    sportType: 0 },
  { id: 'basketball', icon: '🏀', label: 'Basketbol', sportType: 1 },
  { id: 'volleyball', icon: '🏐', label: 'Voleybol',  sportType: 3 }
]

// Spor tipi etiketleri (detay sayfasında kullanılır)
export const SPORT_LABELS = {
  0: '⚽ Futbol',
  1: '🏀 Basketbol',
  2: '🏈 Amerikan Futbolu',
  3: '🏐 Voleybol'
}

// Backend API için spor tipi map'i
export const SPORT_TYPE_MAP = {
  football:   0,
  basketball: 1,
  volleyball: 3
}

// Popüler ligler (dropdown'da öncelikli sıralama için)
export const POPULAR_LEAGUES = [
  // Türkiye
  'Süper Lig', '1. Lig',
  // Avrupa kupaları
  'UEFA Champions League', 'UEFA Europa League', 'UEFA Europa Conference League',
  // Milli takım
  'UEFA Nations League', 'FIFA World Cup', 'UEFA Euro',
  'World Cup - Qualification Europe', 'World Cup - Qualification CONMEBOL',
  'Copa America', 'AFC Asian Cup', 'Africa Cup of Nations',
  // Büyük ligler
  'Premier League', 'La Liga', 'Serie A', 'Bundesliga', 'Ligue 1',
  'Eredivisie', 'Primeira Liga', 'MLS', 'Scottish Premiership', 'Liga MX',
  // Basketbol & voleybol
  'Basketbol Süper Ligi', 'NBA', 'EuroLeague',
  'Efeler Ligi', 'Sultanlar Ligi'
]

/**
 * Bugün maçları olmasa bile puan tablosuna hızlı erişim için
 * sabit popüler lig listesi. StandingsPanel'de gösterilir.
 */
const FLAG = (code) => `https://media.api-sports.io/flags/${code}.svg`

export const POPULAR_STANDINGS_LEAGUES = {
  football: [
    { key: 'Turkey::Süper Lig',              name: 'Süper Lig',              country: 'Turkey',   leagueId: 203, flag: FLAG('tr'), displayName: 'Türkiye - Süper Lig',           collectApiKey: 'super-lig' },
    { key: 'Turkey::1. Lig',                  name: '1. Lig',                 country: 'Turkey',   leagueId: 204, flag: FLAG('tr'), displayName: 'Türkiye - 1. Lig',              collectApiKey: 'tff-1-lig' },
    { key: 'England::Premier League',         name: 'Premier League',         country: 'England',  leagueId: 39,  flag: FLAG('gb'), displayName: 'İngiltere - Premier League',    collectApiKey: 'ingiltere-premier-ligi' },
    { key: 'England::Championship',           name: 'Championship',           country: 'England',  leagueId: 40,  flag: FLAG('gb'), displayName: 'İngiltere - Championship',      collectApiKey: 'ingiltere-sampiyonluk-ligi' },
    { key: 'Spain::La Liga',                  name: 'La Liga',                country: 'Spain',    leagueId: 140, flag: FLAG('es'), displayName: 'İspanya - La Liga',             collectApiKey: 'ispanya-la-liga' },
    { key: 'Italy::Serie A',                  name: 'Serie A',                country: 'Italy',    leagueId: 135, flag: FLAG('it'), displayName: 'İtalya - Serie A',              collectApiKey: 'italya-serie-a-ligi' },
    { key: 'Germany::Bundesliga',             name: 'Bundesliga',             country: 'Germany',  leagueId: 78,  flag: FLAG('de'), displayName: 'Almanya - Bundesliga',          collectApiKey: 'almanya-bundesliga' },
    { key: 'France::Ligue 1',                 name: 'Ligue 1',                country: 'France',   leagueId: 61,  flag: FLAG('fr'), displayName: 'Fransa - Ligue 1',              collectApiKey: 'fransa-ligue-1' },
    { key: 'Germany::2. Bundesliga',          name: '2. Bundesliga',          country: 'Germany',  leagueId: 79,  flag: FLAG('de'), displayName: 'Almanya - 2. Bundesliga',       collectApiKey: 'almanya-bundesliga-2-ligi' },
    { key: 'France::Ligue 2',                 name: 'Ligue 2',                country: 'France',   leagueId: 62,  flag: FLAG('fr'), displayName: 'Fransa - Ligue 2',              collectApiKey: 'fransa-ligue-2' },
    { key: 'Netherlands::Eredivisie',         name: 'Eredivisie',             country: 'Netherlands', leagueId: 88, flag: FLAG('nl'), displayName: 'Hollanda - Eredivisie' },
    { key: 'Portugal::Primeira Liga',         name: 'Primeira Liga',          country: 'Portugal', leagueId: 94,  flag: FLAG('pt'), displayName: 'Portekiz - Primeira Liga' },
    // ── Avrupa Kupaları ───────────────────────────────────────────────────────
    { key: 'World::UEFA Champions League',           name: 'UEFA Champions League',           country: 'World', leagueId: 2,   flag: 'https://media.api-sports.io/football/leagues/2.png',   displayName: 'UEFA Champions League',  collectApiKey: 'sampiyonlar-ligi' },
    { key: 'World::UEFA Europa League',              name: 'UEFA Europa League',              country: 'World', leagueId: 3,   flag: 'https://media.api-sports.io/football/leagues/3.png',   displayName: 'UEFA Europa League',     collectApiKey: 'uefa-avrupa-ligi' },
    { key: 'World::UEFA Europa Conference League',   name: 'UEFA Europa Conference League',   country: 'World', leagueId: 848, flag: 'https://media.api-sports.io/football/leagues/848.png', displayName: 'UEFA Conference League', collectApiKey: 'uefa-konferans-ligi' },
    // ── Milli Takım Turnuvaları ───────────────────────────────────────────────
    { key: 'World::UEFA Nations League',             name: 'UEFA Nations League',             country: 'World', leagueId: 5,   flag: 'https://media.api-sports.io/football/leagues/5.png',   displayName: 'UEFA Nations League' },
    { key: 'World::FIFA World Cup',                  name: 'FIFA World Cup',                  country: 'World', leagueId: 1,   flag: 'https://media.api-sports.io/football/leagues/1.png',   displayName: 'FIFA Dünya Kupası' },
    { key: 'World::UEFA Euro',                       name: 'UEFA Euro',                       country: 'World', leagueId: 4,   flag: 'https://media.api-sports.io/football/leagues/4.png',   displayName: 'UEFA Avrupa Şampiyonası' },
    { key: 'World::World Cup - Qualification Europe',name: 'World Cup - Qualification Europe',country: 'World', leagueId: 33,  flag: 'https://media.api-sports.io/football/leagues/33.png',  displayName: 'Dünya Kupası Eleme (Avrupa)' },
    { key: 'World::Copa America',                    name: 'Copa America',                    country: 'World', leagueId: 9,   flag: 'https://media.api-sports.io/football/leagues/9.png',   displayName: 'Copa America' },
    { key: 'World::Africa Cup of Nations',           name: 'Africa Cup of Nations',           country: 'World', leagueId: 6,   flag: 'https://media.api-sports.io/football/leagues/6.png',   displayName: 'Afrika Uluslar Kupası' },
    // ─────────────────────────────────────────────────────────────────────────
    { key: 'Scotland::Scottish Premiership',  name: 'Scottish Premiership',   country: 'Scotland', leagueId: 179, flag: FLAG('gb'), displayName: 'İskoçya - Premiership' },
    { key: 'Belgium::First Division A',       name: 'First Division A',       country: 'Belgium',  leagueId: 144, flag: FLAG('be'), displayName: 'Belçika - First Division A' },
    { key: 'USA::MLS',                        name: 'MLS',                    country: 'USA',      leagueId: 253, flag: FLAG('us'), displayName: 'ABD - MLS' },
    { key: 'Mexico::Liga MX',                 name: 'Liga MX',                country: 'Mexico',   leagueId: 262, flag: FLAG('mx'), displayName: 'Meksika - Liga MX' },
    { key: 'Greece::Super League',            name: 'Super League',           country: 'Greece',   leagueId: 197, flag: FLAG('gr'), displayName: 'Yunanistan - Super League' },
    { key: 'Russia::Premier League',          name: 'Premier League',         country: 'Russia',   leagueId: 235, flag: FLAG('ru'), displayName: 'Rusya - Premier League' },
    { key: 'Brazil::Série A',                 name: 'Série A',                country: 'Brazil',   leagueId: 71,  flag: FLAG('br'), displayName: 'Brezilya - Série A' },
    { key: 'Argentina::Primera División',     name: 'Primera División',       country: 'Argentina',leagueId: 128, flag: FLAG('ar'), displayName: 'Arjantin - Primera División' },
    { key: 'Saudi Arabia::Roshn League',      name: 'Roshn Saudi League',     country: 'Saudi Arabia', leagueId: 307, flag: FLAG('sa'), displayName: 'S. Arabistan - Roshn League' },
  ],
  basketball: [
    { key: 'Turkey::Basketbol Süper Ligi',   name: 'Basketbol Süper Ligi',   country: 'Turkey',   leagueId: 117, flag: FLAG('tr'), displayName: 'Türkiye - BSL' },
    { key: 'USA::NBA',                        name: 'NBA',                    country: 'USA',       leagueId: 12,  flag: FLAG('us'), displayName: 'ABD - NBA' },
    { key: 'World::EuroLeague',               name: 'EuroLeague',             country: 'World',     leagueId: 120, flag: null,       displayName: 'EuroLeague' },
    { key: 'World::EuroCup',                  name: 'EuroCup',                country: 'World',     leagueId: 121, flag: null,       displayName: 'EuroCup' },
    { key: 'Spain::ACB',                      name: 'ACB',                    country: 'Spain',     leagueId: 119, flag: FLAG('es'), displayName: 'İspanya - ACB' },
    { key: 'France::LNB Pro A',               name: 'LNB Pro A',              country: 'France',    leagueId: 116, flag: FLAG('fr'), displayName: 'Fransa - LNB Pro A' },
    { key: 'Italy::Lega Basket Serie A',      name: 'Lega Basket Serie A',    country: 'Italy',     leagueId: 118, flag: FLAG('it'), displayName: 'İtalya - Lega Basket' },
    { key: 'Germany::BBL',                    name: 'Bundesliga',             country: 'Germany',   leagueId: 114, flag: FLAG('de'), displayName: 'Almanya - BBL' },
  ],
  volleyball: [
    { key: 'Turkey::Efeler Ligi',             name: 'Efeler Ligi',            country: 'Turkey',    leagueId: 186, flag: FLAG('tr'), displayName: 'Türkiye - Efeler Ligi' },
    { key: 'Turkey::Sultanlar Ligi',          name: 'Sultanlar Ligi',         country: 'Turkey',    leagueId: 187, flag: FLAG('tr'), displayName: 'Türkiye - Sultanlar Ligi' },
    { key: 'World::CEV Champions League',     name: 'CEV Champions League',   country: 'World',     leagueId: 180, flag: null,       displayName: 'CEV Şampiyonlar Ligi' },
    { key: 'World::CEV Cup',                  name: 'CEV Cup',                country: 'World',     leagueId: 181, flag: null,       displayName: 'CEV Cup' },
  ]
}

/**
 * API-Football futbol lig ID'leri — externalLeagueId null gelirse fallback olarak kullanılır.
 */
export const FOOTBALL_LEAGUE_IDS = {
  // Türkiye
  'Süper Lig':                         203,
  '1. Lig':                            204,
  'TFF 1. Lig':                        204,
  'Ziraat Türkiye Kupası':             205,

  // İngiltere
  'Premier League':                     39,
  'Championship':                       40,
  'FA Cup':                             45,
  'League Cup':                         48,

  // İspanya
  'La Liga':                           140,
  'Segunda División':                  141,
  'Copa del Rey':                      143,

  // İtalya
  'Serie A':                           135,
  'Serie B':                           136,
  'Coppa Italia':                      137,

  // Almanya
  'Bundesliga':                         78,
  '2. Bundesliga':                      79,
  'DFB Pokal':                          81,

  // Fransa
  'Ligue 1':                            61,
  'Ligue 2':                            62,
  'Coupe de France':                    66,

  // Hollanda
  'Eredivisie':                         88,

  // Portekiz
  'Primeira Liga':                      94,
  'Liga Portugal':                      94,

  // Belçika
  'First Division A':                  144,

  // İskoçya
  'Scottish Premiership':              179,

  // Yunanistan
  'Super League':                      197,

  // Rusya
  'Premier League Russia':             235,

  // Brezilya
  'Série A':                            71,
  'Serie A Brazil':                     71,

  // Arjantin
  'Primera División':                  128,

  // Suudi Arabistan
  'Roshn Saudi League':                307,
  'Saudi Professional League':         307,

  // Amerika
  'MLS':                               253,

  // Meksika
  'Liga MX':                           262,

  // Avrupa / Dünya
  'UEFA Champions League':               2,
  'UEFA Europa League':                  3,
  'UEFA Europa Conference League':     848,
  'UEFA Nations League':                 5,
  'FIFA World Cup':                      1,
  'UEFA Euro':                           4,
  'Copa America':                        9,
  'AFC Asian Cup':                       7,
  'Africa Cup of Nations':               6,
  'World Cup - Qualification Europe':   33,
  'World Cup - Qualification CONMEBOL': 34,
}

/**
 * API-Basketball basketbol lig ID'leri (fallback haritası)
 */
export const BASKETBALL_LEAGUE_IDS = {
  // Türkiye
  'BSL':                      117,
  'Basketbol Süper Ligi':     117,
  // Avrupa / Dünya
  'NBA':                       12,
  'EuroLeague':               120,
  'EuroCup':                  121,
  'FIBA World Cup':            28,
  'FIBA EuroBasket':           35,
  // Diğer
  'ACB':                      119,
  'LNB Pro A':                116,
  'Lega Basket Serie A':      118,
  'Bundesliga':               114,
}

/**
 * API-Volleyball voleybol lig ID'leri (fallback haritası)
 */
export const VOLLEYBALL_LEAGUE_IDS = {
  // Türkiye
  'Efeler Ligi':              186,
  'Sultanlar Ligi':           187,
  // Avrupa / Dünya
  'CEV Champions League':     180,
  'CEV Cup':                  181,
  'Volleyball Nations League': 178,
  'World Championship':       179,
}
