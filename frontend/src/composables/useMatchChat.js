import * as signalR from '@microsoft/signalr'
import { ref, onUnmounted } from 'vue'

/**
 * Maç sohbeti için SignalR bağlantısı.
 * JWT token accessTokenFactory ile iletilir (Clients.User() desteği için).
 * connect() bağlantı kurulana kadar bekleyen bir Promise döner.
 */
export function useMatchChat(token) {
  const connection  = ref(null)
  const isConnected = ref(false)

  // Yeniden bağlanıldığında gruba tekrar katılmak için son matchId saklanır
  let _currentMatchId = null

  /**
   * Bağlantıyı kurar ve event handler'larını kaydeder.
   * @returns {Promise<void>} — bağlantı hazır olduğunda resolve olur
   */
  const connect = async (handlers = {}) => {
    // Dev/Docker: same-origin (nginx veya vite proxy). Vercel: VITE_API_URL ile backend host'u.
    const apiBase = import.meta.env.VITE_API_URL
    const origin = apiBase ? apiBase.replace(/\/$/, '') : window.location.origin
    const hubUrl = `${origin}/matchhub`

    const builder = new signalR.HubConnectionBuilder().withAutomaticReconnect()

    if (token) {
      builder.withUrl(hubUrl, { accessTokenFactory: () => token })
    } else {
      builder.withUrl(hubUrl)
    }

    const conn = builder.build()

    Object.entries(handlers).forEach(([event, handler]) => {
      conn.on(event, handler)
    })

    conn.onreconnected(async () => {
      isConnected.value = true
      // Yeniden bağlanıldığında gruba tekrar katıl
      if (_currentMatchId) {
        try {
          await conn.invoke('JoinMatchChat', _currentMatchId)
        } catch (e) {
          console.warn('Yeniden bağlanma sonrası gruba katılım başarısız:', e)
        }
      }
    })

    conn.onclose(() => {
      isConnected.value = false
    })

    // Bağlantı kurulana kadar bekle
    await conn.start()
    isConnected.value = true
    connection.value  = conn
  }

  const joinMatchChat = async (matchId) => {
    _currentMatchId = matchId
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('JoinMatchChat', matchId)
    }
  }

  const leaveMatchChat = async (matchId) => {
    _currentMatchId = null
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('LeaveMatchChat', matchId)
    }
  }

  const disconnect = () => {
    if (connection.value) {
      connection.value.stop()
      connection.value = null
      isConnected.value = false
    }
  }

  onUnmounted(() => { disconnect() })

  return { connection, isConnected, connect, joinMatchChat, leaveMatchChat, disconnect }
}
