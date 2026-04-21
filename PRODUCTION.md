# SkorTakip — Yayın Öncesi Kontrol Listesi

Bu dosya üretime çıkmadan önce YAPILMASI ZORUNLU olan adımları içerir.
Aşağıdaki her madde tamamlanmadan deploy etmeyin.

---

## 1) Sırlar (secrets) rotasyonu — ZORUNLU

Geliştirme sırasında `.env` dosyasında kullanılan tüm anahtarlar yerel makinede/yedek dizinlerde bulunmuş olabilir.
Üretime çıkmadan önce **tüm anahtarları yenileyin**:

- [ ] **JWT_SECRET** — 32+ karakter, yeni rastgele değer
  ```powershell
  # PowerShell
  [Convert]::ToBase64String((1..48 | %{ Get-Random -Min 0 -Max 255 }))
  ```
- [ ] **POSTGRES_PASSWORD** — güçlü, tahmin edilemez şifre
- [ ] **ADMIN_PASSWORD** — en az 8 karakter, büyük/küçük/rakam
- [ ] **GEMINI_API_KEY** — Google AI Studio'dan yeni anahtar oluşturup eskisini iptal edin
- [ ] **NEWS_API_KEY** — newsdata.io panelinden rotate edin
- [ ] **FOOTBALL/BASKETBALL/VOLLEYBALL_API_KEY** — api-sports.io panelinden rotate edin

## 2) `.env` dosyası kontrolü

- [ ] `.env` dosyası **Git'e eklenmiyor** (`.gitignore` içinde korumalı)
- [ ] Repo geçmişinde `.env` veya gerçek anahtar geçmediğini doğrulayın:
  ```powershell
  git log --all --full-history --oneline -S "GERCEK_ANAHTAR_PARCASI"
  ```
  Hiçbir sonuç dönmemeli.
- [ ] `.env.example` içinde **sadece placeholder** değerler var (kendiliğinden kontrol edildi).

## 3) CORS / izinli origin'ler

- [ ] `ALLOWED_ORIGINS` üretimde **yalnızca gerçek domain**:
  ```
  ALLOWED_ORIGINS=https://skortakip.com,https://www.skortakip.com
  ```
  Localhost üretim .env'sinde olmamalı.

## 4) HTTPS & güvenlik başlıkları

- [ ] Nginx arkasında Let's Encrypt / Cloudflare ile HTTPS aktif.
- [ ] HTTPS aktif olduğunda `frontend/nginx.conf` içindeki **HSTS satırını aç**:
  ```nginx
  add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
  ```
- [ ] CSP başlığındaki `connect-src 'self' ws: wss: https:;` üretimde gerçek API domain'ine daraltılabilir.

## 5) Backend üretim yapılandırması

- [x] `ASPNETCORE_ENVIRONMENT=Production` (docker-compose içinde)
- [x] Swagger sadece Development'ta açılıyor (`Program.cs`).
- [x] SignalR `EnableDetailedErrors` üretimde kapalı.
- [x] JWT secret en az 32 karakter değilse uygulama başlatma anında patlar.
- [x] Rate limiting: auth=10/dk, yorumlar=20/dk, global=300/dk per IP.
- [x] Reverse proxy başlıkları (`X-Forwarded-For`, `X-Forwarded-Proto`) okunur.
- [x] Güvenlik header'ları: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`.

## 6) Yetkilendirme — kritik rota koruması

Kod incelemesi sonrası düzeltilenler:

- [x] `POST/PUT/DELETE /api/match` → `[Authorize(Roles="Admin")]`
- [x] `POST/PATCH /api/admin/*` → `[Authorize(Roles="Admin")]` (zaten vardı)
- [x] `/api/ai/chat` → `[Authorize]` (zaten vardı)
- [x] `/api/favoritematches/*` → `[Authorize]` (zaten vardı)
- [x] `/api/matchcomment` POST/DELETE → `[Authorize]` (GET anonim — herkes okuyabilir)

## 7) Veritabanı

- [ ] Üretim için **ayrı bir Postgres volume/instance** kullanın.
- [ ] `POSTGRES_PASSWORD` güçlü değerle.
- [ ] Port 5432 **dışarı açılmıyor** (docker-compose'da `ports:` yerine sadece `expose`).
- [ ] Düzenli yedek planı: `pg_dump skortakipdb > backup_$(date +%F).sql`

## 8) Docker

- [x] Backend portu dış dünyaya açık değil (sadece nginx proxy).
- [x] Postgres portu açık değil.
- [x] Healthcheck'ler tanımlı (postgres, backend, frontend).
- [ ] İmajları dağıtmadan önce `docker compose build --no-cache` ile temiz build alın.

## 9) Frontend

- [x] Build test edildi — `npm run build` hatasız çalışıyor.
- [x] Global responsive stil (input font=16px ile iOS zoom engellenmiş, safe-area destekli).
- [x] Ana sayfa, header, Admin, MatchDetail, Fixtures, PastMatches, News için breakpoint'ler tanımlı.
- [x] Tüm tablolar `overflow-x: auto` wrapper içinde.
- [ ] Prod sunucuda `/` açıldığında SPA fallback çalıştığını kontrol edin.

## 10) Yayın adımları

```powershell
# 1. .env'yi üretim değerleriyle doldur (sırlar rotate edilmiş olmalı)
Copy-Item .env.example .env
# .env dosyasını düzenleyin

# 2. Docker ile ayağa kaldır
docker compose build --no-cache
docker compose up -d

# 3. Sağlık kontrolü
curl http://localhost:3000/            # frontend
curl http://localhost:3000/api/health  # backend (nginx proxy üzerinden)

# 4. Admin hesabıyla giriş yap, ilk girişte .env'deki ADMIN_PASSWORD'ü panelden DEĞIŞTIR.
```

---

## Bilinen sınırlamalar / ileriki iyileştirmeler

- Email doğrulama henüz yok — kayıt sonrası email doğrulama eklenebilir.
- 2FA (iki faktörlü kimlik doğrulama) eklenebilir.
- İleride `appsettings.Production.json` bir "sentinel config" ile tamamen env-driven hale getirilebilir.
- Sentry/Datadog gibi bir gözlemlenebilirlik aracı entegrasyonu yapılabilir.
