<template>
  <Teleport to="body">
    <div v-if="modelValue" class="modal-backdrop" @click.self="close">
      <div class="modal-card" role="dialog" aria-modal="true">
        <div class="modal-header">
          <h2 class="modal-title">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="title-icon">
              <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
            </svg>
            Şifre Değiştir
          </h2>
          <button class="btn-close" @click="close">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        </div>

        <!-- Chrome autofill'i engellemek için gizli tuzak inputlar -->
        <input type="text" style="display:none" aria-hidden="true"/>
        <input type="password" style="display:none" aria-hidden="true"/>

        <div class="form-group">
          <label class="form-label">Mevcut Şifre</label>
          <div class="input-wrap">
            <input v-model="currentPassword" :type="showCurrent ? 'text' : 'password'" class="input-field" placeholder="••••••••" autocomplete="off" name="cp-current"/>
            <button type="button" class="eye-btn" @click="showCurrent = !showCurrent" tabindex="-1">
              <svg v-if="showCurrent" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>
            </button>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Yeni Şifre</label>
          <div class="input-wrap">
            <input v-model="newPassword" :type="showNew ? 'text' : 'password'" class="input-field" placeholder="En az 6 karakter" autocomplete="off" name="cp-new"/>
            <button type="button" class="eye-btn" @click="showNew = !showNew" tabindex="-1">
              <svg v-if="showNew" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>
            </button>
          </div>
          <div class="strength-row">
            <div class="strength-bar"><div class="strength-fill" :class="strengthClass" :style="{ width: strength + '%' }"></div></div>
            <span v-if="newPassword" class="strength-label" :class="strengthClass">{{ strengthLabel }}</span>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Yeni Şifre (Tekrar)</label>
          <div class="input-wrap" :class="{ mismatch: newPassword && confirmPassword && newPassword !== confirmPassword }">
            <input v-model="confirmPassword" :type="showConfirm ? 'text' : 'password'" class="input-field" placeholder="Şifreyi tekrar gir" autocomplete="off" name="cp-confirm"/>
            <button type="button" class="eye-btn" @click="showConfirm = !showConfirm" tabindex="-1">
              <svg v-if="showConfirm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>
            </button>
          </div>
          <p v-if="newPassword && confirmPassword && newPassword !== confirmPassword" class="field-error">Şifreler eşleşmiyor</p>
        </div>

        <div v-if="errorMessage" class="error-alert">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>
          {{ errorMessage }}
        </div>
        <div v-if="successMessage" class="success-alert">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M9 12l2 2 4-4"/></svg>
          {{ successMessage }}
        </div>

        <div class="modal-actions">
          <button class="btn-cancel" @click="close">İptal</button>
          <button class="btn-primary" :disabled="!canSubmit || loading" @click="handleSubmit">
            <span v-if="loading" class="btn-spinner"></span>
            {{ loading ? 'Güncelleniyor...' : 'Şifreyi Güncelle' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useAuthStore } from '../stores/auth'

const props = defineProps({ modelValue: Boolean })
const emit  = defineEmits(['update:modelValue', 'success'])
const authStore = useAuthStore()

const currentPassword = ref('')
const newPassword     = ref('')
const confirmPassword = ref('')
const showCurrent     = ref(false)
const showNew         = ref(false)
const showConfirm     = ref(false)
const loading         = ref(false)
const errorMessage    = ref('')
const successMessage  = ref('')

const strength = computed(() => {
  const p = newPassword.value
  if (!p) return 0
  let s = 0
  if (p.length >= 6)  s += 25
  if (p.length >= 10) s += 15
  if (/[A-Z]/.test(p)) s += 20
  if (/[0-9]/.test(p)) s += 20
  if (/[^A-Za-z0-9]/.test(p)) s += 20
  return Math.min(s, 100)
})
const strengthClass = computed(() => {
  if (strength.value <= 25) return 'weak'
  if (strength.value <= 50) return 'fair'
  if (strength.value <= 75) return 'good'
  return 'strong'
})
const strengthLabel = computed(() => {
  if (strength.value <= 25) return 'Zayıf'
  if (strength.value <= 50) return 'Orta'
  if (strength.value <= 75) return 'İyi'
  return 'Güçlü'
})

const canSubmit = computed(() =>
  currentPassword.value.length >= 1 &&
  newPassword.value.length >= 6 &&
  newPassword.value === confirmPassword.value
)

const handleSubmit = async () => {
  if (!canSubmit.value || loading.value) return
  loading.value = true
  errorMessage.value = ''
  const result = await authStore.changePassword(currentPassword.value, newPassword.value)
  loading.value = false
  if (result.success) {
    successMessage.value = 'Şifreniz başarıyla güncellendi!'
    setTimeout(() => { emit('success'); close() }, 1500)
  } else {
    errorMessage.value = result.message
  }
}

const reset = () => {
  currentPassword.value = newPassword.value = confirmPassword.value = ''
  showCurrent.value = showNew.value = showConfirm.value = false
  loading.value = false
  errorMessage.value = successMessage.value = ''
}
const close = () => { reset(); emit('update:modelValue', false) }

watch(() => props.modelValue, (v) => { if (!v) reset() })
</script>

<style scoped>
.modal-backdrop {
  position: fixed; inset: 0; background: #00000088; backdrop-filter: blur(4px);
  z-index: 1000; display: flex; align-items: center; justify-content: center; padding: 1rem;
  animation: fadein 0.15s ease;
}
@keyframes fadein { from { opacity: 0 } to { opacity: 1 } }

.modal-card {
  background: #161b22; border: 1px solid #30363d; border-radius: 16px;
  padding: 1.75rem; width: 100%; max-width: 420px;
  animation: slidein 0.2s ease;
}
@keyframes slidein { from { transform: translateY(16px); opacity: 0 } to { transform: none; opacity: 1 } }

.modal-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1.25rem; }
.modal-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.05rem; font-weight: 700; color: #f0f6fc; margin: 0; }
.title-icon { width: 18px; height: 18px; color: #2ECC71; }
.btn-close { background: none; border: none; color: #8b949e; cursor: pointer; padding: 4px; border-radius: 6px; display: flex; }
.btn-close svg { width: 18px; height: 18px; }
.btn-close:hover { color: #f0f6fc; background: #21262d; }

.form-group { margin-bottom: 1rem; }
.form-label { display: block; font-size: 0.82rem; font-weight: 600; color: #c9d1d9; margin-bottom: 0.4rem; }
.input-wrap { display: flex; align-items: center; background: #0d1117; border: 1px solid #30363d; border-radius: 8px; padding: 0 0.75rem; transition: border-color 0.15s; }
.input-wrap:focus-within { border-color: #2ECC71; }
.input-wrap.mismatch { border-color: #f85149; }
.input-field { flex: 1; background: none; border: none; outline: none; color: #f0f6fc; font-size: 0.9rem; padding: 0.65rem 0.5rem; }
.input-field::placeholder { color: #484f58; }
.eye-btn { background: none; border: none; color: #8b949e; cursor: pointer; padding: 0; display: flex; }
.eye-btn svg { width: 16px; height: 16px; }
.eye-btn:hover { color: #c9d1d9; }
.field-error { font-size: 0.78rem; color: #f85149; margin: 4px 0 0; }

.strength-row { display: flex; align-items: center; gap: 0.5rem; margin-top: 6px; }
.strength-bar { flex: 1; height: 4px; background: #21262d; border-radius: 2px; overflow: hidden; }
.strength-fill { height: 100%; border-radius: 2px; transition: width 0.3s, background 0.3s; }
.strength-fill.weak   { background: #f85149; }
.strength-fill.fair   { background: #e3b341; }
.strength-fill.good   { background: #2ECC71; }
.strength-fill.strong { background: #27AE60; }
.strength-label { font-size: 0.75rem; font-weight: 600; min-width: 36px; text-align: right; }
.strength-label.weak   { color: #f85149; }
.strength-label.fair   { color: #e3b341; }
.strength-label.good, .strength-label.strong { color: #2ECC71; }

.error-alert, .success-alert { display: flex; align-items: center; gap: 0.5rem; padding: 0.6rem 0.8rem; border-radius: 8px; font-size: 0.83rem; margin-bottom: 1rem; }
.error-alert   { background: #f8514915; border: 1px solid #f8514940; color: #f85149; }
.success-alert { background: #2ECC7115; border: 1px solid #2ECC7140; color: #2ECC71; }
.error-alert svg, .success-alert svg { width: 15px; height: 15px; flex-shrink: 0; }

.modal-actions { display: flex; gap: 0.75rem; margin-top: 0.5rem; }
.btn-cancel { flex: 1; padding: 0.7rem; background: #21262d; border: 1px solid #30363d; border-radius: 8px; color: #c9d1d9; font-size: 0.9rem; cursor: pointer; transition: background 0.15s; }
.btn-cancel:hover { background: #30363d; }
.btn-primary { flex: 2; padding: 0.7rem; background: #2ECC71; border: none; border-radius: 8px; color: #0d1117; font-weight: 700; font-size: 0.9rem; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 0.4rem; transition: background 0.15s, opacity 0.15s; }
.btn-primary:hover:not(:disabled) { background: #27AE60; }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-spinner { width: 15px; height: 15px; border: 2px solid #0d111740; border-top-color: #0d1117; border-radius: 50%; animation: spin 0.7s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>
