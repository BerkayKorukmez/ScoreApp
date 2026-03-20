/**
 * Tarih ve metin biçimlendirme yardımcı fonksiyonları
 * Tüm view'larda ortak kullanılır
 */
export function useFormatters() {
  /** Maç saatini biçimlendir (ör: "20:45") */
  const formatTime = (dateString) => {
    const date = new Date(dateString)
    return date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
  }

  /** Tarih ve saati biçimlendir (ör: "05 Mart 2026, 20:45") */
  const formatDateTime = (dateString) => {
    if (!dateString) return '-'
    const date = new Date(dateString)
    return date.toLocaleDateString('tr-TR', {
      day: '2-digit',
      month: 'long',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  /** Göreceli tarih biçimlendirme (ör: "5 dk önce", "3 saat önce") */
  const formatRelativeDate = (dateString) => {
    if (!dateString) return ''
    const date = new Date(dateString)
    const now = new Date()
    const diff = now - date
    const minutes = Math.floor(diff / (1000 * 60))
    const hours = Math.floor(diff / (1000 * 60 * 60))

    if (minutes < 60) return `${minutes} dk önce`
    if (hours < 24) return `${hours} saat önce`
    if (hours < 48) return 'Dün'

    return date.toLocaleDateString('tr-TR', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    })
  }

  /** Metni belirli uzunlukta kısalt */
  const truncateText = (text, maxLen) => {
    if (!text) return ''
    if (text.length <= maxLen) return text
    return text.substring(0, maxLen).trim() + '...'
  }

  return { formatTime, formatDateTime, formatRelativeDate, truncateText }
}
