import http from './http'

/**
 * Chat geçmişini getirir (giriş gerekli).
 * @returns {Promise<Array<{ role: string, text: string }>>}
 */
export const getAiChatHistory = async () => {
  const { data } = await http.get('/ai/chat')
  return Array.isArray(data) ? data : []
}

/**
 * Mesaj gönderir, yanıt döner (giriş gerekli).
 * @param {string} message - Kullanıcı mesajı
 * @returns {Promise<{ reply: string }>}
 */
export const sendAiMessage = async (message) => {
  const { data } = await http.post('/ai/chat', { message })
  return data
}

/**
 * Chat geçmişini siler (giriş gerekli).
 */
export const clearAiChat = async () => {
  await http.delete('/ai/chat')
}
