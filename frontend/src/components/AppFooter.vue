<template>
  <footer class="app-footer">
    <div class="footer-inner">

      <!-- Marka -->
      <div class="footer-brand">
        <div class="brand-row">
          <svg class="brand-icon" viewBox="0 0 36 36" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
            <circle cx="18" cy="18" r="17" fill="#2ECC71"/>
            <text x="18" y="24.5" text-anchor="middle" font-family="'Arial Black', Arial, sans-serif"
                  font-size="19" font-weight="900" fill="#ffffff" letter-spacing="-1">S</text>
          </svg>
          <span class="brand-name">SkorNet</span>
        </div>
        <p class="brand-desc">
          Futbol, basketbol ve voleybol maçlarını canlı takip et.
          Puan durumları, fikstür, istatistikler ve spor haberleri tek platformda.
        </p>
      </div>

      <!-- Hakkımızda -->
      <div class="footer-section">
        <h4 class="section-title">Hakkımızda</h4>
        <p class="section-text">
          SkorNet, sporseverlerin maçları anlık olarak takip etmesi için geliştirilmiş
          bir spor portalıdır. Canlı skor güncellemeleri, puan tabloları, geçmiş maç
          sonuçları ve yapay zeka destekli maç analizleriyle sporu yakından hissettiriyoruz.
        </p>
      </div>

      <!-- İletişim -->
      <div class="footer-section">
        <h4 class="section-title">İletişim</h4>
        <ul class="contact-list">
          <li>
            <a
              href="https://tr.linkedin.com/in/berkay-k%C3%B6r%C3%BCkmez-9392572b2"
              target="_blank"
              rel="noopener noreferrer"
              class="contact-link linkedin"
            >
              <svg class="contact-icon" viewBox="0 0 24 24" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
                <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 0 1-2.063-2.065 2.064 2.064 0 1 1 2.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
              </svg>
              <span>Berkay Körükmez</span>
            </a>
          </li>
        </ul>
      </div>

    </div>

    <!-- Alt çizgi -->
    <div class="footer-bottom">
      <span>© {{ new Date().getFullYear() }} SkorNet. Tüm hakları saklıdır.</span>
      <span class="footer-note">Veriler üçüncü taraf API'lerden sağlanmaktadır.</span>
    </div>
  </footer>
</template>

<script setup>
</script>

<style scoped>
.app-footer {
  background: #0d1117;
  border-top: 1px solid #21262d;
  margin-top: auto;
}

.footer-inner {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2.5rem 1.5rem 1.5rem;
  display: grid;
  grid-template-columns: 1.4fr 1.6fr 1fr;
  gap: 2.5rem;
}

/* ── Marka ── */
.footer-brand {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.brand-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.brand-icon {
  width: 30px;
  height: 30px;
  flex-shrink: 0;
}

.brand-name {
  font-size: 1.15rem;
  font-weight: 700;
  color: #f0f6fc;
  letter-spacing: -0.3px;
}

.brand-desc {
  font-size: 0.82rem;
  color: #8b949e;
  line-height: 1.6;
  max-width: 280px;
}

/* ── Bölümler ── */
.footer-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.section-title {
  font-size: 0.78rem;
  font-weight: 600;
  color: #f0f6fc;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  margin: 0;
}

.section-text {
  font-size: 0.82rem;
  color: #8b949e;
  line-height: 1.65;
  margin: 0;
}

/* ── İletişim ── */
.contact-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.contact-link {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.85rem;
  color: #8b949e;
  text-decoration: none;
  transition: color 0.15s;
  padding: 0.3rem 0;
}

.contact-link:hover {
  color: #f0f6fc;
}

.contact-link.linkedin:hover {
  color: #0a66c2;
}

.contact-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

/* ── Alt çizgi ── */
.footer-bottom {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1rem 1.5rem;
  border-top: 1px solid #21262d;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  font-size: 0.75rem;
  color: #484f58;
  flex-wrap: wrap;
}

/* ── Responsive ── */
@media (max-width: 900px) {
  .footer-inner {
    grid-template-columns: 1fr 1fr;
    gap: 2rem;
  }
  .footer-brand {
    grid-column: 1 / -1;
  }
}

@media (max-width: 560px) {
  .footer-inner {
    grid-template-columns: 1fr;
    padding: 2rem 1rem 1rem;
    gap: 1.75rem;
  }
  .footer-bottom {
    flex-direction: column;
    align-items: flex-start;
    padding: 0.85rem 1rem;
    gap: 0.25rem;
  }
}
</style>
