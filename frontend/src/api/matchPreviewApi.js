import http from './http'

/**
 * Oynanacak maç AI önizlemesi — /api/ai sohbetinden bağımsız uç (POST /api/match-preview).
 */
export const fetchMatchPreview = (body) =>
  http.post('/match-preview', body)
