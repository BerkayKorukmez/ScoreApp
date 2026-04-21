import * as signalR from '@microsoft/signalr'
import { ref, onUnmounted } from 'vue'

/**
 * SignalR WebSocket bağlantı composable'ı
 * Canlı skor güncellemeleri için kullanılır
 */
export function useWebSocket() {
  const connection = ref(null)
  const isConnected = ref(false)

  /**
   * WebSocket bağlantısını başlat
   * @param {Object} handlers - { eventName: handlerFunction } şeklinde olay dinleyicileri
   * @returns {signalR.HubConnection}
   */
  const connect = (handlers = {}) => {
    // Dev (vite proxy) / Docker (nginx proxy): same-origin. Vercel prod: VITE_API_URL ile backend host'u.
    const apiBase = import.meta.env.VITE_API_URL
    const origin = apiBase ? apiBase.replace(/\/$/, '') : window.location.origin
    const hubUrl = `${origin}/matchhub`

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build()

    // Olay dinleyicilerini kaydet
    Object.entries(handlers).forEach(([event, handler]) => {
      conn.on(event, handler)
    })

    conn.onreconnected(() => {
      isConnected.value = true
      console.log('WebSocket yeniden bağlandı ✓')
    })

    conn.onclose(() => {
      isConnected.value = false
    })

    conn.start()
      .then(() => {
        isConnected.value = true
        console.log('WebSocket bağlantısı kuruldu ✓')
      })
      .catch(err => console.error('WebSocket bağlantı hatası:', err))

    connection.value = conn
    return conn
  }

  /** WebSocket bağlantısını kapat */
  const disconnect = () => {
    if (connection.value) {
      connection.value.stop()
      connection.value = null
      isConnected.value = false
    }
  }

  // Component unmount olduğunda otomatik kapat
  onUnmounted(() => {
    disconnect()
  })

  return { connection, isConnected, connect, disconnect }
}
