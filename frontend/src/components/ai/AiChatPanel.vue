<template>
  <div class="ai-chat-wrapper">
    <button
      type="button"
      class="ai-chat-trigger"
      :class="{ active: isOpen }"
      @click="toggle"
      title="Spor asistanı ile sohbet"
    >
      <span class="ai-icon">🤖</span>
      <span class="ai-label">AI Sohbet</span>
    </button>

    <Teleport to="body">
      <Transition name="ai-panel">
        <div v-if="isOpen" class="ai-chat-overlay" @click="close">
          <div class="ai-chat-panel" @click.stop>
            <div class="ai-chat-header">
              <h3 class="ai-chat-title">⚽ Spor Asistanı</h3>
              <p class="ai-chat-sub">Spor ile ilgili sorularınızı sorun</p>
              <div class="ai-chat-header-actions">
                <button
                  type="button"
                  class="ai-chat-clear"
                  :class="{ disabled: messages.length === 0 }"
                  :disabled="messages.length === 0 || isLoading"
                  @click="clearChat"
                  title="Sohbeti sil"
                >
                  🗑️ Sil
                </button>
                <button type="button" class="ai-chat-close" @click="close" aria-label="Kapat">×</button>
              </div>
            </div>

            <div class="ai-chat-messages" ref="messagesEl">
              <div v-if="isLoadingHistory" class="ai-chat-welcome">
                <p>Geçmiş yükleniyor...</p>
              </div>
              <div v-else-if="messages.length === 0" class="ai-chat-welcome">
                <p>Merhaba! Ben spor asistanınızım.</p>
                <p>Örnek sorular:</p>
                <ul>
                  <li>Galatasaray maçı ne olur?</li>
                  <li>Bu hafta Süper Lig'de hangi maçlar var?</li>
                  <li>Messi kaç gol attı?</li>
                </ul>
              </div>
              <div
                v-for="(msg, i) in messages"
                :key="i"
                class="ai-message"
                :class="msg.role"
              >
                <span class="ai-msg-avatar">{{ msg.role === 'user' ? '👤' : '🤖' }}</span>
                <div class="ai-msg-content">
                  <p v-if="msg.role === 'user'" class="ai-msg-text">{{ msg.text }}</p>
                  <p v-else class="ai-msg-text" v-html="formatReply(msg.text)"></p>
                </div>
              </div>
              <div v-if="isLoading" class="ai-message assistant">
                <span class="ai-msg-avatar">🤖</span>
                <div class="ai-msg-content">
                  <div class="ai-typing">
                    <span></span><span></span><span></span>
                  </div>
                </div>
              </div>
            </div>

            <form class="ai-chat-form" @submit.prevent="send">
              <input
                v-model="inputText"
                type="text"
                class="ai-chat-input"
                placeholder="Soru sorun... (örn: Galatasaray maçı ne olur?)"
                :disabled="isLoading"
                maxlength="500"
              />
              <button
                type="submit"
                class="ai-chat-send"
                :disabled="!inputText.trim() || isLoading"
              >
                {{ isLoading ? '...' : 'Gönder' }}
              </button>
            </form>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, watch, nextTick } from 'vue'
import { getAiChatHistory, sendAiMessage, clearAiChat } from '../../api/aiApi'

const isOpen = ref(false)
const messages = ref([])
const inputText = ref('')
const isLoading = ref(false)
const isLoadingHistory = ref(false)
const messagesEl = ref(null)

const toggle = () => {
  isOpen.value = !isOpen.value
}

const close = () => {
  isOpen.value = false
}

const loadHistory = async () => {
  isLoadingHistory.value = true
  messages.value = []
  try {
    messages.value = await getAiChatHistory()
  } catch {
    messages.value = []
  } finally {
    isLoadingHistory.value = false
  }
}

const clearChat = async () => {
  if (isLoading.value) return
  try {
    await clearAiChat()
    messages.value = []
  } catch {
    messages.value = []
  }
}

const formatReply = (text) => {
  if (!text) return ''
  return text
    .replace(/\n/g, '__BR__')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/__BR__/g, '<br>')
}

const send = async () => {
  const text = inputText.value.trim()
  if (!text || isLoading.value) return

  messages.value.push({ role: 'user', text })
  inputText.value = ''
  isLoading.value = true

  try {
    const { reply } = await sendAiMessage(text)
    messages.value.push({ role: 'assistant', text: reply })
  } catch (err) {
    messages.value.push({
      role: 'assistant',
      text: 'Üzgünüm, bir hata oluştu. Lütfen tekrar deneyin.'
    })
  } finally {
    isLoading.value = false
    await nextTick()
    messagesEl.value?.scrollTo({ top: messagesEl.value.scrollHeight, behavior: 'smooth' })
  }
}

watch(isOpen, async (open) => {
  if (open) {
    await loadHistory()
    await nextTick()
    messagesEl.value?.scrollTo({ top: messagesEl.value.scrollHeight, behavior: 'smooth' })
  }
})
</script>

<style scoped>
.ai-chat-trigger {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.35rem 0.75rem;
  background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
  color: #fff;
  border: none;
  border-radius: 20px;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.ai-chat-trigger:hover {
  transform: scale(1.02);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.4);
}

.ai-chat-trigger.active {
  box-shadow: 0 0 0 2px #a5b4fc;
}

.ai-icon {
  font-size: 0.9rem;
}

.ai-label {
  white-space: nowrap;
}

.ai-chat-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
  padding: 1rem;
}

.ai-chat-panel {
  width: 100%;
  max-width: 440px;
  max-height: 85vh;
  background: #161b22;
  border-radius: 16px;
  box-shadow: 0 24px 48px rgba(0, 0, 0, 0.4);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid #21262d;
}

.ai-chat-header {
  padding: 1rem 1.25rem;
  background: linear-gradient(135deg, #1e1b4b 0%, #312e81 100%);
  border-bottom: 1px solid #3730a3;
  position: relative;
}

.ai-chat-title {
  margin: 0 0 0.25rem 0;
  font-size: 1rem;
  font-weight: 700;
  color: #e0e7ff;
}

.ai-chat-sub {
  margin: 0;
  font-size: 0.75rem;
  color: #a5b4fc;
}

.ai-chat-header-actions {
  position: absolute;
  top: 1rem;
  right: 1rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.ai-chat-clear {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.35rem 0.6rem;
  background: rgba(248, 81, 73, 0.2);
  border: 1px solid rgba(248, 81, 73, 0.4);
  border-radius: 8px;
  color: #f85149;
  font-size: 0.75rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.ai-chat-clear:hover:not(:disabled) {
  background: rgba(248, 81, 73, 0.3);
}

.ai-chat-clear:disabled,
.ai-chat-clear.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.ai-chat-close {
  width: 28px;
  height: 28px;
  background: rgba(255, 255, 255, 0.1);
  border: none;
  border-radius: 8px;
  color: #c7d2fe;
  font-size: 1.2rem;
  line-height: 1;
  cursor: pointer;
  transition: all 0.2s;
}

.ai-chat-close:hover {
  background: rgba(255, 255, 255, 0.2);
}

.ai-chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
  min-height: 200px;
  max-height: 400px;
}

.ai-chat-welcome {
  color: #8b949e;
  font-size: 0.85rem;
  line-height: 1.6;
}

.ai-chat-welcome p {
  margin: 0 0 0.5rem 0;
}

.ai-chat-welcome ul {
  margin: 0.5rem 0 0 1.25rem;
  padding: 0;
}

.ai-chat-welcome li {
  margin: 0.25rem 0;
}

.ai-message {
  display: flex;
  gap: 0.6rem;
  margin-bottom: 1rem;
}

.ai-message.user {
  flex-direction: row-reverse;
}

.ai-msg-avatar {
  font-size: 1.2rem;
  flex-shrink: 0;
}

.ai-msg-content {
  max-width: 85%;
}

.ai-msg-text {
  margin: 0;
  padding: 0.6rem 0.9rem;
  font-size: 0.9rem;
  line-height: 1.5;
  border-radius: 12px;
}

.ai-message.user .ai-msg-text {
  background: #27AE60;
  color: #fff;
  border-bottom-right-radius: 4px;
}

.ai-message.assistant .ai-msg-text {
  background: #21262d;
  color: #e6edf3;
  border-bottom-left-radius: 4px;
}

.ai-typing {
  display: flex;
  gap: 4px;
  padding: 0.75rem 1rem;
}

.ai-typing span {
  width: 8px;
  height: 8px;
  background: #58a6ff;
  border-radius: 50%;
  animation: ai-bounce 1.4s ease-in-out infinite both;
}

.ai-typing span:nth-child(1) { animation-delay: -0.32s; }
.ai-typing span:nth-child(2) { animation-delay: -0.16s; }

@keyframes ai-bounce {
  0%, 80%, 100% { transform: scale(0); }
  40% { transform: scale(1); }
}

.ai-chat-form {
  display: flex;
  gap: 0.5rem;
  padding: 1rem;
  border-top: 1px solid #21262d;
  background: #0d1117;
}

.ai-chat-input {
  flex: 1;
  padding: 0.6rem 1rem;
  background: #21262d;
  border: 1px solid #30363d;
  border-radius: 10px;
  color: #e6edf3;
  font-size: 0.9rem;
  outline: none;
  transition: border-color 0.2s;
}

.ai-chat-input:focus {
  border-color: #6366f1;
}

.ai-chat-input::placeholder {
  color: #6e7681;
}

.ai-chat-send {
  padding: 0.6rem 1.2rem;
  background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
  color: #fff;
  border: none;
  border-radius: 10px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.ai-chat-send:hover:not(:disabled) {
  opacity: 0.9;
  transform: translateY(-1px);
}

.ai-chat-send:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.ai-panel-enter-active,
.ai-panel-leave-active {
  transition: opacity 0.2s ease;
}

.ai-panel-enter-from,
.ai-panel-leave-to {
  opacity: 0;
}

.ai-panel-enter-active .ai-chat-panel,
.ai-panel-leave-active .ai-chat-panel {
  transition: transform 0.2s ease;
}

.ai-panel-enter-from .ai-chat-panel,
.ai-panel-leave-to .ai-chat-panel {
  transform: scale(0.95);
}

@media (max-width: 600px) {
  .ai-label {
    display: none;
  }
}
</style>
