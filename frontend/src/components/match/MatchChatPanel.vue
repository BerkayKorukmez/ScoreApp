<template>
  <div class="chat-panel">
    <!-- Ban uyarısı -->
    <div v-if="isBanned" class="ban-notice">
      <span class="ban-icon">🚫</span>
      <p>Sohbetten yasaklandınız. Yorum yapamazsınız.</p>
    </div>

    <!-- Yorum listesi -->
    <div ref="listEl" class="comments-list">
      <div v-if="isLoading" class="chat-state">Yorumlar yükleniyor...</div>
      <div v-else-if="comments.length === 0" class="chat-state">
        Henüz yorum yok. İlk yorumu sen yap!
      </div>

      <div
        v-for="comment in comments"
        :key="comment.id"
        class="comment-item"
        :class="{ 'own-comment': comment.userId === currentUserId, 'pending-comment': comment._temp }"
      >
        <div class="comment-header">
          <span class="comment-username">{{ comment.userName }}</span>
          <span class="comment-time">{{ formatTime(comment.createdAt) }}</span>
          <div class="comment-actions">
            <button
              v-if="isAdmin && comment.userId !== currentUserId && !comment._temp"
              class="action-btn ban-btn"
              title="Kullanıcıyı yasakla"
              @click="openBanDialog(comment.userId, comment.userName)"
            >
              🚫
            </button>
            <button
              v-if="(isAdmin || comment.userId === currentUserId) && !comment._temp"
              class="action-btn delete-btn"
              title="Yorumu sil"
              @click="openDeleteDialog(comment.id)"
            >
              🗑
            </button>
          </div>
        </div>
        <p class="comment-content">{{ comment.content }}</p>
      </div>
    </div>

    <!-- Hata mesajı -->
    <div v-if="chatError" class="chat-error-notice" @click="chatError = ''">
      ⚠️ {{ chatError }}
    </div>

    <!-- Giriş yap uyarısı -->
    <div v-if="!isAuthenticated" class="chat-login-notice">
      Yorum yapmak için <router-link to="/login" class="login-link">giriş yapın</router-link>.
    </div>

    <!-- Yorum yazma alanı -->
    <form
      v-else-if="!isBanned"
      class="comment-form"
      @submit.prevent="handleSubmit"
    >
      <input
        v-model="newContent"
        type="text"
        class="comment-input"
        placeholder="Yorumunuzu yazın… (max 500 karakter)"
        maxlength="500"
        :disabled="isSending"
        autocomplete="off"
      />
      <button
        type="submit"
        class="send-btn"
        :disabled="!newContent.trim() || isSending"
      >
        Gönder
      </button>
    </form>
  </div>

  <!-- ─── Onay Modal'ı ─── -->
  <Teleport to="body">
    <Transition name="modal-fade">
      <div v-if="dialog.open" class="chat-modal-overlay" @click.self="closeDialog">
        <div class="chat-modal" role="dialog" :aria-label="dialog.title">
          <div class="chat-modal-icon">{{ dialog.icon }}</div>
          <h3 class="chat-modal-title">{{ dialog.title }}</h3>
          <p class="chat-modal-desc">{{ dialog.description }}</p>
          <div class="chat-modal-actions">
            <button class="modal-btn modal-btn-cancel" @click="closeDialog">
              İptal
            </button>
            <button
              class="modal-btn"
              :class="dialog.confirmClass"
              :disabled="dialog.loading"
              @click="dialog.onConfirm"
            >
              <span v-if="dialog.loading" class="modal-spinner"></span>
              <span v-else>{{ dialog.confirmLabel }}</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, reactive, nextTick, onMounted, onUnmounted } from 'vue'
import { useMatchChat } from '../../composables/useMatchChat'
import { fetchComments, postComment, deleteComment } from '../../api/commentApi'
import { banUserFromChat } from '../../api/adminApi'

const props = defineProps({
  matchId:         { type: String,  required: true },
  isAuthenticated: { type: Boolean, default: false },
  isAdmin:         { type: Boolean, default: false },
  currentUserId:   { type: String,  default: null },
  currentUserName: { type: String,  default: 'Ben' },
  token:           { type: String,  default: null }
})

const comments   = ref([])
const newContent = ref('')
const isLoading  = ref(true)
const isSending  = ref(false)
const isBanned   = ref(false)
const chatError  = ref('')
const listEl     = ref(null)

// ─── Onay modal durumu ────────────────────────────────────────────────────────
const dialog = reactive({
  open:         false,
  icon:         '',
  title:        '',
  description:  '',
  confirmLabel: 'Onayla',
  confirmClass: 'modal-btn-danger',
  loading:      false,
  onConfirm:    () => {}
})

const closeDialog = () => {
  if (dialog.loading) return
  dialog.open = false
}

const openDeleteDialog = (commentId) => {
  Object.assign(dialog, {
    open:         true,
    icon:         '🗑️',
    title:        'Yorumu Sil',
    description:  'Bu yorumu silmek istediğinize emin misiniz? Bu işlem geri alınamaz.',
    confirmLabel: 'Evet, Sil',
    confirmClass: 'modal-btn-danger',
    loading:      false,
    onConfirm:    () => confirmDelete(commentId)
  })
}

const openBanDialog = (userId, userName) => {
  Object.assign(dialog, {
    open:         true,
    icon:         '🚫',
    title:        'Kullanıcıyı Yasakla',
    description:  `"${userName}" kullanıcısını sohbetten yasaklamak istediğinize emin misiniz?`,
    confirmLabel: 'Evet, Yasakla',
    confirmClass: 'modal-btn-warning',
    loading:      false,
    onConfirm:    () => confirmBan(userId)
  })
}

const confirmDelete = async (id) => {
  dialog.loading = true
  try {
    await deleteComment(id)
    dialog.open = false
  } catch {
    chatError.value = 'Yorum silinemedi.'
    dialog.open = false
  }
}

const confirmBan = async (userId) => {
  dialog.loading = true
  try {
    await banUserFromChat(userId)
    dialog.open = false
  } catch (err) {
    chatError.value = err.response?.data?.message || 'Kullanıcı yasaklanamadı.'
    dialog.open = false
  }
}

// ─── Chat bağlantısı ──────────────────────────────────────────────────────────
const { connect, joinMatchChat, leaveMatchChat } = useMatchChat(props.token)

const scrollToBottom = () => {
  nextTick(() => {
    if (listEl.value) listEl.value.scrollTop = listEl.value.scrollHeight
  })
}

const formatTime = (iso) => {
  const d = new Date(iso)
  return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

const hasComment = (id) => comments.value.some((c) => c.id === id)

onMounted(async () => {
  try {
    comments.value = await fetchComments(props.matchId)
  } catch (e) {
    console.error('Yorumlar yüklenemedi:', e)
  } finally {
    isLoading.value = false
    scrollToBottom()
  }

  try {
    await connect({
      NewComment: (comment) => {
        const tempIdx = comments.value.findIndex(
          (c) => c._temp === true && c.content === comment.content && c.userId === comment.userId
        )
        if (tempIdx !== -1) {
          comments.value.splice(tempIdx, 1, comment)
          return
        }
        if (!hasComment(comment.id)) {
          comments.value.push(comment)
          scrollToBottom()
        }
      },
      CommentDeleted: (id) => {
        comments.value = comments.value.filter((c) => c.id !== id)
      },
      ChatBanned: () => {
        isBanned.value = true
      },
      ChatUnbanned: () => {
        isBanned.value = false
      }
    })

    await joinMatchChat(props.matchId)
  } catch (e) {
    console.error('Sohbet bağlantısı kurulamadı:', e)
  }
})

onUnmounted(async () => {
  await leaveMatchChat(props.matchId)
})

const handleSubmit = async () => {
  const content = newContent.value.trim()
  if (!content || isSending.value) return

  chatError.value  = ''
  isSending.value  = true
  newContent.value = ''

  const tempComment = {
    _temp:     true,
    id:        `temp-${Date.now()}`,
    matchId:   props.matchId,
    userId:    props.currentUserId,
    userName:  props.currentUserName,
    content,
    createdAt: new Date().toISOString()
  }
  comments.value.push(tempComment)
  scrollToBottom()

  try {
    const saved = await postComment(props.matchId, content)
    const tempIdx = comments.value.findIndex((c) => c.id === tempComment.id)
    if (tempIdx !== -1) comments.value.splice(tempIdx, 1, saved)
  } catch (err) {
    comments.value = comments.value.filter((c) => c.id !== tempComment.id)
    newContent.value = content
    const msg = err.response?.data?.message
    if (msg?.includes('yasaklandınız')) {
      isBanned.value = true
    } else {
      chatError.value = msg || 'Yorum gönderilemedi. Lütfen tekrar deneyin.'
    }
  } finally {
    isSending.value = false
  }
}
</script>

<style scoped>
.chat-panel {
  display: flex;
  flex-direction: column;
  height: 500px;
  background: #0d1117;
  border-radius: 8px;
  overflow: hidden;
}

/* ─── Hata / Ban bildirimleri ─────────────────────────────────────────────── */
.chat-error-notice {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #f8514922;
  border-bottom: 1px solid #f8514966;
  padding: 0.5rem 1rem;
  color: #f85149;
  font-size: 0.82rem;
  cursor: pointer;
}

.ban-notice {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #f8514922;
  border-bottom: 1px solid #f8514966;
  padding: 0.6rem 1rem;
  color: #f85149;
  font-size: 0.85rem;
}
.ban-notice p { margin: 0; }
.ban-icon { font-size: 1.1rem; }

/* ─── Yorum listesi ───────────────────────────────────────────────────────── */
.comments-list {
  flex: 1;
  overflow-y: auto;
  padding: 0.75rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.chat-state {
  color: #484f58;
  font-size: 0.85rem;
  text-align: center;
  padding: 2rem 0;
}

.comment-item {
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 8px;
  padding: 0.5rem 0.75rem;
  max-width: 100%;
}

.comment-item.own-comment {
  background: #1c2840;
  border-color: #58a6ff33;
  align-self: flex-end;
  width: 80%;
}

.pending-comment { opacity: 0.55; }

.comment-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.25rem;
}

.comment-username {
  font-weight: 600;
  font-size: 0.8rem;
  color: #58a6ff;
  flex: 1;
}

.comment-time {
  font-size: 0.72rem;
  color: #484f58;
}

.comment-actions {
  display: flex;
  gap: 0.25rem;
  opacity: 0;
  transition: opacity 0.15s;
}

.comment-item:hover .comment-actions { opacity: 1; }

.action-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 0.8rem;
  padding: 0.15rem 0.25rem;
  border-radius: 4px;
  line-height: 1;
  transition: background 0.15s;
}

.action-btn:hover { background: #21262d; }

.comment-content {
  margin: 0;
  font-size: 0.88rem;
  color: #c9d1d9;
  word-break: break-word;
  white-space: pre-wrap;
}

/* ─── Giriş ve form ───────────────────────────────────────────────────────── */
.chat-login-notice {
  padding: 0.75rem 1rem;
  font-size: 0.85rem;
  color: #8b949e;
  text-align: center;
  border-top: 1px solid #21262d;
  background: #161b22;
}

.login-link { color: #58a6ff; text-decoration: none; }
.login-link:hover { text-decoration: underline; }

.comment-form {
  display: flex;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: #161b22;
  border-top: 1px solid #21262d;
}

.comment-input {
  flex: 1;
  background: #0d1117;
  border: 1px solid #30363d;
  color: #c9d1d9;
  padding: 0.5rem 0.75rem;
  border-radius: 6px;
  font-size: 0.85rem;
  outline: none;
  transition: border-color 0.2s;
}
.comment-input::placeholder { color: #484f58; }
.comment-input:focus { border-color: #58a6ff; }

.send-btn {
  background: #238636;
  color: #fff;
  border: 1px solid #2ea043;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;
  white-space: nowrap;
}
.send-btn:hover:not(:disabled) { background: #2ea043; }
.send-btn:disabled { opacity: 0.5; cursor: not-allowed; }

/* ─── Onay Modal ─────────────────────────────────────────────────────────── */
.chat-modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.65);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
}

.chat-modal {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 12px;
  padding: 2rem 1.75rem 1.5rem;
  max-width: 360px;
  width: 100%;
  text-align: center;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.6);
}

.chat-modal-icon {
  font-size: 2.2rem;
  margin-bottom: 0.75rem;
  line-height: 1;
}

.chat-modal-title {
  margin: 0 0 0.5rem;
  font-size: 1rem;
  font-weight: 700;
  color: #e6edf3;
}

.chat-modal-desc {
  margin: 0 0 1.5rem;
  font-size: 0.85rem;
  color: #8b949e;
  line-height: 1.5;
}

.chat-modal-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
}

.modal-btn {
  padding: 0.55rem 1.25rem;
  border-radius: 7px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.15s;
  min-width: 90px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
}

.modal-btn:disabled { opacity: 0.55; cursor: not-allowed; }

.modal-btn-cancel {
  background: #21262d;
  color: #c9d1d9;
  border-color: #30363d;
}
.modal-btn-cancel:hover:not(:disabled) { background: #30363d; }

.modal-btn-danger {
  background: #b91c1c;
  color: #fff;
  border-color: #dc2626;
}
.modal-btn-danger:hover:not(:disabled) { background: #dc2626; }

.modal-btn-warning {
  background: #9a3412;
  color: #fff;
  border-color: #ea580c;
}
.modal-btn-warning:hover:not(:disabled) { background: #ea580c; }

.modal-spinner {
  display: inline-block;
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

/* ─── Modal geçiş animasyonu ─────────────────────────────────────────────── */
.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.18s ease;
}
.modal-fade-enter-active .chat-modal,
.modal-fade-leave-active .chat-modal {
  transition: transform 0.18s ease, opacity 0.18s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}
.modal-fade-enter-from .chat-modal,
.modal-fade-leave-to .chat-modal {
  transform: scale(0.92) translateY(-8px);
  opacity: 0;
}
</style>
