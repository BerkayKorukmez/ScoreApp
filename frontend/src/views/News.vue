<template>
  <div class="news-page">

    <!-- Sayfa Başlığı ve Filtreler -->
    <div class="news-header">
      <div class="news-title-section">
        <h1 class="news-page-title">📰 Spor Haberleri</h1>
        <p class="news-page-desc">Dünya sporlarından en güncel haberler</p>
      </div>

      <div class="news-filters">
        <!-- Dil Seçimi -->
        <div class="filter-group">
          <label class="filter-label">Dil</label>
          <select v-model="selectedLanguage" class="filter-select" @change="loadNews(true)">
            <option value="tr">🇹🇷 Türkçe</option>
            <option value="en">🇬🇧 İngilizce</option>
            <option value="de">🇩🇪 Almanca</option>
            <option value="fr">🇫🇷 Fransızca</option>
            <option value="es">🇪🇸 İspanyolca</option>
            <option value="it">🇮🇹 İtalyanca</option>
          </select>
        </div>

        <!-- Arama -->
        <div class="filter-group search-group">
          <label class="filter-label">Ara</label>
          <div class="search-wrapper">
            <input
              v-model="searchQuery"
              type="text"
              class="search-input"
              placeholder="Futbol, basketbol, F1..."
              @keyup.enter="loadNews(true)"
            />
            <button class="search-btn" @click="loadNews(true)">🔍</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Yükleniyor -->
    <div v-if="isLoading && articles.length === 0" class="loading-state">
      <div class="spinner"></div>
      <span>Haberler yükleniyor...</span>
    </div>

    <!-- Hata Durumu -->
    <div v-else-if="errorMessage" class="error-state">
      <span class="error-icon">⚠️</span>
      <p>{{ errorMessage }}</p>
      <button class="retry-btn" @click="loadNews(true)">Tekrar Dene</button>
    </div>

    <!-- Haber Bulunamadı -->
    <div v-else-if="articles.length === 0 && !isLoading" class="empty-state">
      <span class="empty-icon">📭</span>
      <p>Haber bulunamadı</p>
      <small>Farklı bir arama terimi veya dil seçmeyi deneyin</small>
    </div>

    <!-- HABER KARTLARI -->
    <div v-else class="news-grid">
      <!-- Öne Çıkan Haber (İlk haber büyük kart) -->
      <div
        v-if="articles.length > 0"
        class="featured-card"
        @click="openArticle(articles[0].link)"
      >
        <div class="featured-image-wrapper">
          <img
            v-if="articles[0].image_url"
            :src="articles[0].image_url"
            :alt="articles[0].title"
            class="featured-image"
            @error="handleImageError"
          />
          <div v-else class="featured-image-placeholder">
            <span>📰</span>
          </div>
          <div class="featured-overlay">
            <div class="featured-meta">
              <span v-if="articles[0].source_name" class="news-source">{{ articles[0].source_name }}</span>
              <span class="news-date">{{ formatRelativeDate(articles[0].pubDate) }}</span>
            </div>
            <h2 class="featured-title">{{ articles[0].title }}</h2>
            <p v-if="articles[0].description" class="featured-desc">
              {{ truncateText(articles[0].description, 200) }}
            </p>
          </div>
        </div>
      </div>

      <!-- Normal Haber Kartları -->
      <div
        v-for="(article, index) in articles.slice(1)"
        :key="article.article_id || index"
        class="news-card"
        @click="openArticle(article.link)"
      >
        <div class="card-image-wrapper">
          <img
            v-if="article.image_url"
            :src="article.image_url"
            :alt="article.title"
            class="card-image"
            @error="handleImageError"
          />
          <div v-else class="card-image-placeholder">
            <span>📰</span>
          </div>
        </div>
        <div class="card-content">
          <div class="card-meta">
            <span v-if="article.source_name" class="news-source">{{ article.source_name }}</span>
            <span class="news-date">{{ formatRelativeDate(article.pubDate) }}</span>
          </div>
          <h3 class="card-title">{{ article.title }}</h3>
          <p v-if="article.description" class="card-desc">
            {{ truncateText(article.description, 120) }}
          </p>
          <div class="card-footer">
            <div v-if="article.category" class="card-categories">
              <span
                v-for="cat in article.category"
                :key="cat"
                class="category-tag"
              >
                {{ cat }}
              </span>
            </div>
            <span class="read-more">Devamını Oku →</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Daha Fazla Yükle Butonu -->
    <div v-if="nextPageToken && articles.length > 0" class="load-more-wrapper">
      <button class="load-more-btn" :disabled="isLoadingMore" @click="loadMoreNews">
        <template v-if="isLoadingMore">
          <div class="spinner-sm"></div>
          Yükleniyor...
        </template>
        <template v-else>
          Daha Fazla Haber Yükle
        </template>
      </button>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { fetchSportsNews } from '../api/newsApi'
import { useFormatters } from '../composables/useFormatters'

const { formatRelativeDate, truncateText } = useFormatters()

/* =============================================
   DURUM DEĞİŞKENLERİ
   ============================================= */
const articles = ref([])
const isLoading = ref(false)
const isLoadingMore = ref(false)
const errorMessage = ref('')
const selectedLanguage = ref('tr')
const searchQuery = ref('')
const nextPageToken = ref(null)

/* =============================================
   HABERLERİ YÜKLE
   ============================================= */
const loadNews = async (reset = false) => {
  if (reset) {
    articles.value = []
    nextPageToken.value = null
  }

  isLoading.value = true
  errorMessage.value = ''

  try {
    const query = searchQuery.value.trim() || null
    const data = await fetchSportsNews(selectedLanguage.value, query, null)

    if (data.status === 'success' && data.results) {
      articles.value = data.results
      nextPageToken.value = data.nextPage || null
    } else {
      articles.value = []
      errorMessage.value = 'Haberler yüklenirken bir sorun oluştu.'
    }
  } catch (error) {
    console.error('Haber yükleme hatası:', error)
    if (error.response?.status === 429) {
      errorMessage.value = 'API istek limiti aşıldı. Lütfen biraz bekleyip tekrar deneyin.'
    } else {
      errorMessage.value = 'Haberler yüklenirken bir hata oluştu. Lütfen tekrar deneyin.'
    }
  } finally {
    isLoading.value = false
  }
}

/* =============================================
   DAHA FAZLA HABER YÜKLE (Sayfalama)
   ============================================= */
const loadMoreNews = async () => {
  if (!nextPageToken.value || isLoadingMore.value) return

  isLoadingMore.value = true

  try {
    const query = searchQuery.value.trim() || null
    const data = await fetchSportsNews(selectedLanguage.value, query, nextPageToken.value)

    if (data.status === 'success' && data.results) {
      articles.value.push(...data.results)
      nextPageToken.value = data.nextPage || null
    }
  } catch (error) {
    console.error('Daha fazla haber yükleme hatası:', error)
  } finally {
    isLoadingMore.value = false
  }
}

/* =============================================
   YARDIMCI FONKSİYONLAR
   ============================================= */
const openArticle = (url) => {
  if (url) window.open(url, '_blank')
}

const handleImageError = (event) => {
  event.target.style.display = 'none'
}

/* =============================================
   YAŞAM DÖNGÜSÜ
   ============================================= */
onMounted(() => {
  loadNews(true)
})
</script>

<style scoped>
/* =============================================
   HABER SAYFASI
   ============================================= */
.news-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1.5rem;
}

.news-header { margin-bottom: 2rem; }

.news-title-section { margin-bottom: 1.25rem; }

.news-page-title {
  font-size: 1.6rem;
  font-weight: 800;
  color: #ffffff;
  margin: 0 0 0.35rem 0;
}

.news-page-desc {
  font-size: 0.9rem;
  color: #8b949e;
  margin: 0;
}

/* =============================================
   FİLTRELER
   ============================================= */
.news-filters {
  display: flex;
  gap: 1rem;
  align-items: flex-end;
  flex-wrap: wrap;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.filter-label {
  font-size: 0.72rem;
  font-weight: 600;
  color: #8b949e;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.filter-select {
  background: #161b22;
  color: #c9d1d9;
  border: 1px solid #30363d;
  padding: 0.55rem 0.85rem;
  border-radius: 8px;
  font-size: 0.85rem;
  cursor: pointer;
  outline: none;
  transition: border-color 0.2s;
  min-width: 150px;
}

.filter-select:focus { border-color: #58a6ff; }
.filter-select option { background: #161b22; }

.search-group { flex: 1; min-width: 250px; }
.search-wrapper { display: flex; gap: 0; }

.search-input {
  flex: 1;
  background: #161b22;
  color: #c9d1d9;
  border: 1px solid #30363d;
  border-right: none;
  padding: 0.55rem 0.85rem;
  border-radius: 8px 0 0 8px;
  font-size: 0.85rem;
  outline: none;
  transition: border-color 0.2s;
}

.search-input:focus { border-color: #58a6ff; }
.search-input::placeholder { color: #484f58; }

.search-btn {
  background: #21262d;
  border: 1px solid #30363d;
  border-left: none;
  color: #c9d1d9;
  padding: 0.55rem 0.85rem;
  border-radius: 0 8px 8px 0;
  cursor: pointer;
  font-size: 0.9rem;
  transition: background 0.2s;
}

.search-btn:hover { background: #30363d; }

/* =============================================
   YÜKLENİYOR, HATA, BOŞ DURUMLAR
   ============================================= */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 4rem;
  color: #8b949e;
  font-size: 0.9rem;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid #21262d;
  border-top-color: #58a6ff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.spinner-sm {
  width: 16px;
  height: 16px;
  border: 2px solid #21262d;
  border-top-color: #58a6ff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 4rem;
  text-align: center;
}

.error-icon { font-size: 2.5rem; }
.error-state p { color: #f85149; font-size: 0.95rem; font-weight: 500; }

.retry-btn {
  background: #21262d;
  color: #58a6ff;
  border: 1px solid #30363d;
  padding: 0.5rem 1.25rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.retry-btn:hover { background: #30363d; }

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 4rem;
  color: #8b949e;
  text-align: center;
}

.empty-icon { font-size: 2.5rem; opacity: 0.5; }
.empty-state p { font-size: 1rem; font-weight: 500; }
.empty-state small { font-size: 0.8rem; color: #484f58; }

/* =============================================
   HABER GRID
   ============================================= */
.news-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 1.25rem;
}

/* =============================================
   ÖNE ÇIKAN HABER KARTI
   ============================================= */
.featured-card {
  grid-column: 1 / -1;
  cursor: pointer;
  border-radius: 12px;
  overflow: hidden;
  transition: transform 0.25s, box-shadow 0.25s;
}

.featured-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.4);
}

.featured-image-wrapper {
  position: relative;
  width: 100%;
  min-height: 400px;
  background: #161b22;
}

.featured-image {
  width: 100%;
  height: 400px;
  object-fit: cover;
}

.featured-image-placeholder {
  width: 100%;
  height: 400px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #161b22, #21262d);
  font-size: 4rem;
  opacity: 0.3;
}

.featured-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: linear-gradient(transparent, rgba(0, 0, 0, 0.85));
  padding: 2.5rem 2rem 1.75rem;
}

.featured-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.65rem;
}

.featured-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #ffffff;
  line-height: 1.35;
  margin: 0 0 0.5rem 0;
}

.featured-desc {
  font-size: 0.9rem;
  color: #b1bac4;
  line-height: 1.55;
  margin: 0;
}

/* =============================================
   NORMAL HABER KARTI
   ============================================= */
.news-card {
  background: #161b22;
  border: 1px solid #21262d;
  border-radius: 12px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.25s;
  display: flex;
  flex-direction: column;
}

.news-card:hover {
  border-color: #30363d;
  transform: translateY(-3px);
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.3);
}

.card-image-wrapper {
  width: 100%;
  height: 200px;
  overflow: hidden;
  flex-shrink: 0;
}

.card-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}

.news-card:hover .card-image { transform: scale(1.05); }

.card-image-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1c2129, #21262d);
  font-size: 2.5rem;
  opacity: 0.3;
}

.card-content {
  padding: 1rem 1.15rem;
  display: flex;
  flex-direction: column;
  flex: 1;
}

.card-meta {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 0.5rem;
}

.news-source {
  background: #58a6ff18;
  color: #58a6ff;
  padding: 0.15rem 0.55rem;
  border-radius: 4px;
  font-size: 0.7rem;
  font-weight: 600;
}

.news-date {
  font-size: 0.72rem;
  color: #8b949e;
}

.card-title {
  font-size: 1rem;
  font-weight: 600;
  color: #e1e4e8;
  line-height: 1.4;
  margin: 0 0 0.5rem 0;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.news-card:hover .card-title { color: #58a6ff; }

.card-desc {
  font-size: 0.82rem;
  color: #8b949e;
  line-height: 1.5;
  margin: 0 0 0.75rem 0;
  flex: 1;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: auto;
  padding-top: 0.65rem;
  border-top: 1px solid #21262d;
}

.card-categories { display: flex; gap: 0.35rem; flex-wrap: wrap; }

.category-tag {
  background: #21262d;
  color: #8b949e;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
  font-size: 0.68rem;
  font-weight: 500;
  text-transform: capitalize;
}

.read-more {
  font-size: 0.78rem;
  color: #58a6ff;
  font-weight: 500;
  white-space: nowrap;
}

/* =============================================
   DAHA FAZLA YÜKLE
   ============================================= */
.load-more-wrapper {
  display: flex;
  justify-content: center;
  padding: 2rem 0;
}

.load-more-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #21262d;
  color: #58a6ff;
  border: 1px solid #30363d;
  padding: 0.75rem 2rem;
  border-radius: 10px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.load-more-btn:hover:not(:disabled) {
  background: #30363d;
  border-color: #58a6ff55;
}

.load-more-btn:disabled { opacity: 0.6; cursor: not-allowed; }

/* =============================================
   RESPONSIVE
   ============================================= */
@media (max-width: 900px) {
  .news-grid { grid-template-columns: 1fr; }

  .featured-image,
  .featured-image-placeholder { height: 280px; }

  .featured-title { font-size: 1.2rem; }
}

@media (max-width: 600px) {
  .news-page { padding: 1rem; }
  .news-filters { flex-direction: column; }
  .search-group { min-width: unset; width: 100%; }

  .featured-image,
  .featured-image-placeholder { height: 220px; }

  .featured-title { font-size: 1.05rem; }
  .featured-overlay { padding: 1.5rem 1rem 1rem; }
  .card-image-wrapper { height: 160px; }
}
</style>
