/**
 * Merkezi HTTP istemci modülü.
 *
 * Tüm API dosyaları axios'u doğrudan import etmek yerine bu modülü kullanmalıdır.
 * - axios.defaults.baseURL '/api' olarak main.js'de ayarlanmıştır.
 * - JWT interceptor'lar auth.js store tarafından global instance üzerinde kurulur.
 * İleride custom instance'a geçmek gerekirse sadece bu dosyayı değiştirmek yeterlidir.
 */
import axios from 'axios'

export default axios
