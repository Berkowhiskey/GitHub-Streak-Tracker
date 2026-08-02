# 🔥 StreakTracker

GitHub commit serini takip eder, seri bozulmak üzereyken **GitHub Mobile üzerinden telefonuna push bildirimi** gönderir ve profil README'ne ekleyebileceğin **dinamik SVG rozet** üretir.

**🌐 Canlı:** [streak-tracker.me](https://streak-tracker.me) · **API:** [api.streak-tracker.me](https://api.streak-tracker.me/health) · **Durum:** MVP yayında, aylık $0 maliyetle çalışıyor

```markdown
![GitHub Streak](https://api.streak-tracker.me/api/v1/badges/KULLANICI_ADIN.svg)
```

| Parametre | Değerler |
|---|---|
| `theme` | `dark` (varsayılan) · `light` · `dracula` · `tokyo-night` · `nord` · `catppuccin` |
| `variant` | `full` (varsayılan) · `compact` · `max` (README'de tam genişlik) |
| `lang` | `tr` · `en` — verilmezse hesabındaki tercih kullanılır |
| `animated` | `false` ile alev animasyonu kapatılır |

Alev canlı olarak yanıp söner; işletim sisteminde "hareketi azalt" seçiliyse animasyon kendiliğinden durur. Seriye göre bir de **rütbe** kazanırsın: Kıvılcım (1) → Alev (7) → Ateş (30) → Yangın (100) → Efsane (365).

Panelden **Rozeti Özelleştir** ile alev renklerini, arka planı ve kenarlığı da değiştirebilirsin. Kaydettiğinde adrese kısa bir imza (`?s=a4f2`) eklenir — bu, GitHub'ın rozet önbelleğinin güncel görünümü çekmesini sağlar.

Projenin tam mimarisi, fazları ve ayrıntılı ilerleme günlüğü: [CLAUDE.md](CLAUDE.md)

---

## Nasıl çalışıyor?

**1. Bildirim hilesi.** Hesabında `.streak-tracker-notifications` adlı **gizli** bir repo açılır. Seri tehlikedeyse bu repodaki sabit bir Issue'ya yorum düşer; GitHub Mobile onu anında push bildirimine çevirir.

> ⚠️ **Kritik ayrıntı:** Yorumu **bot kimliği** (GitHub App) atmak zorundadır. GitHub, kullanıcının *kendi* eylemleri için ona bildirim göndermez — kendi kendini `@mention` etse bile. Bu yüzden bildirim akışı GitHub App üzerinden yürür.

**2. Kesintisiz rozet.** Rozet doğrudan veritabanından render edilir, istek anında GitHub API'sine **hiç gidilmez**. Bu yüzden rate-limit'e takılmaz; GitHub yavaşlasa bile rozet çalışır.

**3. Şeffaf onboarding.** Onay verilmeden hesabında hiçbir şey oluşturulmaz. Access token veritabanında şifreli tutulur, panelden tek tıkla tüm veriler silinebilir.

**4. Senin saatin, senin dilin.** Bildirim saati kendi saat diliminde saklanır — yaz/kış saati değişse bile seçtiğin saatte uyarılırsın. Arayüz, bildirimler ve rozet **Türkçe ve İngilizce** çalışır.

---

## Teknoloji

| Katman | Kullanılan |
|---|---|
| Backend | ASP.NET Core 9 · EF Core · Hangfire · Octokit |
| Frontend | Next.js 16 · React 19 · TypeScript · Tailwind v4 · shadcn/ui |
| Veritabanı | PostgreSQL |
| Kimlik | GitHub OAuth 2.0 · JWT (HttpOnly çerez) · GitHub App |

---

## Yerel Geliştirme

**Gereksinimler:** .NET 9 SDK · Docker Desktop · Node.js 20+

```bash
# 1) PostgreSQL'i ayağa kaldır (localhost:5434)
docker compose up -d

# 2) Yerel ayarlarını oluştur
cp backend/src/StreakTracker.API/appsettings.Development.example.json \
   backend/src/StreakTracker.API/appsettings.Development.json
#    → GitHub OAuth App ve GitHub App bilgilerini doldur

# 3) Backend
dotnet run --project backend/src/StreakTracker.API --launch-profile http

# 4) Frontend (ayrı terminal)
cd frontend && npm install && npm run dev
```

| Adres | |
|---|---|
| http://localhost:3000 | Arayüz |
| http://localhost:5157/health | Sağlık kontrolü |
| http://localhost:5157/swagger | API dokümantasyonu *(yalnızca Development)* |
| http://localhost:5157/hangfire | Arka plan görev paneli |

> PostgreSQL **5434** portundadır (5432/5433 başka projeler tarafından kullanılıyordu).

### Testler
```bash
dotnet test backend/StreakTracker.sln     # 169 test
cd frontend && npx tsc --noEmit           # tip kontrolü
```

### Migration
```bash
cd backend/src/StreakTracker.API
dotnet ef migrations add <Ad> --output-dir Data/Migrations
dotnet ef database update
```
Development ortamında bekleyen migration'lar her açılışta otomatik uygulanır.

---

## Production

Yayındaki kurulum **aylık $0** maliyetle çalışır:

| Katman | Servis |
|---|---|
| API + Caddy | Oracle Cloud Always Free (AMD `E2.1.Micro`, Ubuntu 24.04) |
| Veritabanı | Supabase (Supavisor **session pooler**, port 5432) |
| Frontend | Vercel |
| SSL / Proxy | Caddy — Let's Encrypt sertifikasını otomatik alır ve yeniler |

### Sunucuda deploy

```bash
git clone <repo> && cd GitHub-Streak-Tracker
cp .env.example .env && chmod 600 .env      # değerleri doldur
mkdir -p secrets                            # GitHub App .pem dosyasını içine koy

# Container root olmayan kullanıcı (uid 5678) olarak çalışır; bind mount host
# izinlerini koruduğu için sahiplik bu kullanıcıya verilmelidir.
sudo chown -R 5678:5678 secrets
sudo chmod 700 secrets && sudo chmod 400 secrets/*.pem

docker compose -f docker-compose.prod.yml up -d --build
```

Güncelleme:
```bash
git pull && docker compose -f docker-compose.prod.yml up -d --build
```

### Production'da dikkat edilmesi gerekenler

- **`ForwardedHeaders` zorunlu.** Caddy arkasında uygulama HTTP konuşur; bu middleware olmadan `Request.IsHttps` false döner, çerez `Secure` işaretlenmez ve **giriş hiç çalışmaz.**
- **DataProtection anahtarları kalıcı volume'de** (`/keys`). Kaybolurlarsa kayıtlı access token'lar çözülemez ve tüm kullanıcılar yeniden giriş yapmak zorunda kalır.
- **Supabase'de session mode (5432) kullanılmalı.** Transaction mode (6543) prepared statement desteklemez; EF Core ve Hangfire bozulur.
- **Çerez politikası** `App:CookieSameSite` ile ayarlanır. Frontend ile API aynı site altındaysa `Lax`, farklı alan adlarındaysa `None` (+ HTTPS zorunlu).
- **Hangfire dashboard** production'da yalnızca loopback'ten erişilebilir. Gerekirse SSH tüneli kullanın.
- **`secrets/` klasörünün sahibi container kullanıcısı (uid 5678) olmalı.** Aksi halde GitHub App anahtarı okunamaz ve uygulama sessizce "App yapılandırılmamış" der — yapılandırma doğru olsa bile.

---

## Yol Haritası

**Yapıldı:** GitHub OAuth + onboarding · streak hesaplama (GraphQL) · GitHub App ile bot bildirimleri · saatlik `StreakCheckJob` · dinamik SVG rozet (ETag/cache) · Next.js dashboard (heatmap, rozet kopyalama, bildirim ayarları) · token şifreleme · KVKK silme hakkı · production deploy · **saat dilimi desteği** (IANA, DST'ye dayanıklı) · **milestone bildirimleri** (7/30/100/365) · **Türkçe + İngilizce dil desteği** (arayüz, bildirimler, rozet) · **rozet paketi** (animasyonlu alev, 6 tema, rütbe sistemi, kompakt boyut)

**Sırada:**
- [ ] **Telegram / e-posta fallback** — `NotificationChannel` enum'ında yer var, uygulanmadı
- [ ] **Streak dondurma** — tatil/hastalık için seri koruma hakkı
- [ ] **Public profil sayfası** (`/u/{username}`) — rozete tıklayınca gidilecek bir yer
- [ ] **Dashboard'da milestone ilerleme göstergesi** — "7 güne 3 gün kaldı"
- [ ] **Yıl özeti (Wrapped)** — paylaşılabilir yıllık özet görseli
- [ ] **Haftalık özet** — pazar günü "bu hafta 5/7 gün" raporu
- [ ] **Public profil sayfası** (`/u/{username}`) ve leaderboard
- [ ] **Rozet çeşitleri** — kompakt sürüm, ek temalar
- [ ] `NotificationService` / `GitHubAppService` için birim testleri

---

## Proje Yapısı

```
GitLingo/
├── backend/
│   ├── StreakTracker.sln
│   ├── Dockerfile · .dockerignore
│   ├── src/StreakTracker.API/
│   │   ├── Controllers/     Auth · Onboarding · Users · Streaks · Notifications · Badge
│   │   ├── Services/        GitHub · GitHubApp · Streak · Notification · SvgBadge · Auth
│   │   │                    UserClock (saat dilimi) · StreakCalculator · NotificationMessageBuilder
│   │   ├── Jobs/            StreakCheckJob (saatlik)
│   │   ├── Data/            AppDbContext · Configurations · Migrations
│   │   └── Entities/        User · Streak · NotificationLog
│   └── tests/StreakTracker.Tests/
├── frontend/                Next.js (App Router)
│   ├── app/                 landing · onboarding · dashboard · dashboard/rozet · gizlilik
│   ├── components/          heatmap · copy-field · app-install-notice · icons
│   │                        language-provider · language-switcher
│   ├── lib/i18n.ts          tr/en sözlükleri (tr şema görevi görür)
│   └── lib/api.ts           backend DTO'larıyla eşleşen istemci
├── docker-compose.yml       geliştirme (PostgreSQL)
├── docker-compose.prod.yml  production (api + caddy)
├── Caddyfile
└── CLAUDE.md                mimari + ilerleme günlüğü
```
