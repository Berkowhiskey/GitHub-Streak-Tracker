# 🚀 GitHub Streak Tracker & Notifier - Detaylı Proje Yönergeleri (CLAUDE.md)

Bu dosya, yapay zeka kod asistanları (özellikle Claude Code CLI) için projenin tüm teknik detaylarını, uçtan uca mimarisini, fazlara bölünmüş geliştirme adımlarını ve **ilerleme kayıtlarını** barındıran ana rehberdir.

---

## 🎯 Proje Vizyonu ve Mantığı
Geliştiricilerin GitHub üzerindeki "streak" (kesintisiz günlük kod yazma serisi) durumlarını takip eden, streak bozulmak üzereyken (örn. akşam 20:00 UTC'de) **GitHub'ın kendi altyapısını ve mobil uygulamasını kullanarak push bildirimi atan** ve profillerine ekleyebilecekleri **%100 Uptime garantili dinamik SVG rozetleri (badges)** üreten SaaS platformu.

### 💡 Temel Hileler ve Mimari Kararlar
1. **GitHub Mobil Push Bildirim Hilesi:** Kullanıcı kaydolduğunda hesabında `.streak-tracker-notifications` adında **Private (Gizli)** bir repo oluşturulur. Bildirim atılacağı zaman bu repodaki sabit bir Issue'ya `@kullaniciadi` etiketiyle yorum atılır. GitHub Mobile uygulaması bunu anlık push bildirimi olarak kullanıcının telefonuna düşürür.
2. **Kapanmayan Dinamik SVG Badge:** Harici 3. taraf servisler (demolab vb.) GitHub API kısıtlarına takılıp çöker. Bizim servisimiz `/api/v1/badges/{username}.svg` adresi üzerinden doğrudan veritabanımızdaki verilerle milisaniyeler içinde SVG render eder; çökme, gecikme yaşanmaz.
3. **1-Click Onboarding & KVKK:** Kullanıcı GitHub OAuth ile giriş yapar, gizli repo ve bildirim izin metnini tek tıkla onaylar, arka planda tüm kurulum otomatik tamamlanır.

---

## 🛠 Teknoloji Yığını (Tech Stack)

* **Backend:** ASP.NET Core 8/9 Web API
* **ORM & Database:** Entity Framework Core (PostgreSQL veya SQL Server)
* **GitHub Entegrasyonu:** Octokit.net, GitHub GraphQL API & REST API
* **Zamanlanmış Görevler:** Quartz.NET veya Hangfire
* **Frontend:** Next.js (App Router), TypeScript, Tailwind CSS, shadcn/ui
* **Kimlik Doğrulama:** GitHub OAuth 2.0 / JWT

---

## 📋 DETAYLI GELİŞTİRME FAZLARI VE ADIMLAR (YOL HARİTASI)

### 🔹 FAZ 1: Backend Altyapısı ve Veritabanı Mimarisi (ASP.NET Core)
* **Adım 1.1: Proje Kurulumu ve Paketler**
  * `StreakTracker.API` Web API projesinin oluşturulması.
  * Gerekli NuGet paketlerinin yüklenmesi: `Octokit`, `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL` (veya SQL Server), `Hangfire.AspNetCore` / `Quartz`.
* **Adım 1.2: Domain Modellerinin (Entities) Yazılması**
  * `Entities/User.cs`: ID, GitHubUsername, GitHubId, Email, AccessToken, PrivateNotificationRepoName, NotificationIssueNumber, PreferredNotificationHourUtc, IsActive.
  * `Entities/Streak.cs`: ID, UserId, CurrentStreak, LongestStreak, LastCommitDate, HasCommittedToday, LastCheckedAt.
  * `Entities/NotificationLog.cs`: ID, UserId, Channel (GitHubIssue, Telegram, Email), Message, IsSuccess, ErrorMessage, SentAt.
* **Adım 1.3: DbContext ve Migration**
  * `Data/AppDbContext.cs` sınıfının ve Fluent API ilişkilerinin yapılandırılması.
  * `InitialCreate` EF Core migration'ının oluşturulması ve veritabanı şemasının hazırlanması.

### 🔹 FAZ 2: GitHub API Entegrasyonu ve Servis Katmanı
* **Adım 2.1: GitHub API Servisi (`IGitHubService`)**
  * `Octokit` istemcisinin yönetimi (AccessToken ile yetkilendirme).
  * Kullanıcının gün içinde commit/push yapıp yapmadığını sorgulayan metot (`HasUserCommittedTodayAsync`).
  * Otomatik gizli repo oluşturma metodu (`CreatePrivateNotificationRepoAsync`).
  * Gizli repoda bildirim Issue'su açma metodu (`EnsureNotificationIssueExistsAsync`).
* **Adım 2.2: Streak Hesaplama Servisi (`IStreakService`)**
  * Kullanıcının streak verilerini günlük güncelleyen mantık (`UpdateUserStreakAsync`).
  * Seri kırıldı mı, devam mı ediyor kontrolü.

### 🔹 FAZ 3: Arka Plan Görevleri ve Bildirim Motoru
* **Adım 3.1: Zamanlayıcı Yapılandırması (Background Jobs)**
  * Quartz.NET / Hangfire entegrasyonu.
  * Her saat başı veya belirli UTC saatlerinde tetiklenen `StreakCheckJob` oluşturulması.
* **Adım 3.2: Bildirim Servisi (`INotificationService`)**
  * Günlük commit yapmamış kullanıcıları tespit etme.
  * `GitHubService` üzerinden gizli repo Issue'suna `@kullaniciadi Hey! Streak'in tehlikede! 🔥` yorumu göndererek mobil push bildirimi tetikleme.
  * Fallback kanalları (Telegram Bot / E-posta) entegrasyonu hazırlığı.

### 🔹 FAZ 4: Dinamik SVG Rozet (Badge) Servisi
* **Adım 4.1: SVG Generator Engine (`ISvgBadgeService`)**
  * C# tarafında in-memory şık SVG şablonları üretme.
  * Dark mode / Light mode temaları, alev simgeleri ve güncel streak sayısını vektörel çizme.
* **Adım 4.2: Badge Endpoint (`BadgeController`)**
  * `GET /api/v1/badges/{username}.svg` endpoint'inin yazılması.
  * Yanıt türünün `image/svg+xml` olarak dönülmesi ve HTTP caching header'larının (ETag, Cache-Control) yapılandırılması.

### 🔹 FAZ 5: Auth & API Controller Katmanı
* **Adım 5.1: GitHub OAuth Entegrasyonu (`AuthController`)**
  * GitHub OAuth akışı, access token alma ve JWT üretimi.
  * Giriş anında KVKK/Onboarding işleminin (gizli repo oluşturma) tetiklenmesi.
* **Adım 5.2: Kullanıcı ve Streak Endpoint'leri (`UserController`, `StreakController`)**
  * Frontend için profil, mevcut streak durumu, bildirim tercihleri ve rozet HTML/Markdown kodlarını dönen API'ler.

### 🔹 FAZ 6: Frontend Arayüzü (Next.js + Tailwind CSS + shadcn/ui)
* **Adım 6.1: Next.js Proje Kurulumu**
  * TypeScript, App Router, Tailwind CSS ve shadcn/ui bileşen kütüphanesinin kurulumu.
  * Dark Mode temanın varsayılan yapılması.
* **Adım 6.2: Landing & Onboarding Sayfaları**
  * Karşılama ekranı, "GitHub ile Giriş Yap" butonu.
  * Açık ve şeffaf Onboarding/KVKK onay kutusu (Gizli repo oluşturma bilgilendirmesi).
* **Adım 6.3: Dashboard Arayüzü**
  * Canlı streak istatistikleri kartları.
  * Takvim / Heatmap görünümü.
  * Profil README'sine eklenecek SVG Rozet kopyalama alanı (Markdown & HTML formatında).
  * Bildirim saati ve kanalı ayarları paneli.

---

## 👨‍💻 CLAUDE CODE İÇİN KODLAMA VE MİMARİ STANDARTLARI

1. **Temiz Kod ve Katmanlar:** C# tarafında Controller -> Service -> Repository/DbContext ayrımına uyulmalıdır. Spagetti kod yazılmamalıdır.
2. **Error Handling & Resilience:** GitHub API rate-limit hatalarına karşı `try-catch` blokları ve loglama mekanizması aktif tutulmalıdır.
3. **Stateless Badge Generator:** SVG üretimi tamamen stateless olmalı, doğrudan veritabanından veri okuyup resim basmalıdır.
4. **Asenkron Yapı:** Bütün I/O ve API/DB işlemleri `async/await` pattern'ine uygun yazılmalıdır.

---

## 📝 PROJE İLERLEME VE TAKİP GÜNLÜĞÜ (CHANGELOG)

**DİKKAT CLAUDE:** Tamamlanan her adım, eklenen her yeni dosya veya çözülen her hata aşağıya **Tarih ve Saat:Dakika** formatında kaydedilmelidir. Gün gün ve adım adım ilerlemeyi buradan takip edeceğiz.

### 📅 Log Kayıtları

* **28 Temmuz 2026, 11:15** - Proje fikir aşaması tamamlandı, temel mimari belirlendi.
* **28 Temmuz 2026, 11:16** - Ilk C# Entity modelleri taslak olarak konuşuldu.
* **28 Temmuz 2026, 12:28** - `CLAUDE.md` dosyası uçtan uca fazlara ve detaylı adımlara bölünerek baştan yazıldı. Proje tamamen Claude Code ile adım adım inşa edilecek şekilde yapılandırıldı.

---

#### 🔹 FAZ 1 — Backend Altyapısı ve Veritabanı Mimarisi

* **28 Temmuz 2026, 13:05** - **Mimari kararlar netleştirildi:** Veritabanı olarak **PostgreSQL (Docker)**, arka plan görevleri için **Hangfire** seçildi (dashboard'lu olması geliştirme sürecini kolaylaştırdığı için Quartz.NET yerine tercih edildi). Hedef framework: **.NET 9**.
* **28 Temmuz 2026, 13:08** - Proje klasörü `git` reposu olarak başlatıldı (`main` branch). `.gitignore` oluşturuldu; `appsettings.Development.json`, `.env` ve tüm sır içeren dosyalar hariç tutuldu.
* **28 Temmuz 2026, 13:12** - **[Adım 1.1]** `backend/StreakTracker.sln` solution'ı ve `backend/src/StreakTracker.API` Web API projesi oluşturuldu. Template'in `WeatherForecast` artıkları temizlendi.
* **28 Temmuz 2026, 13:15** - **[Adım 1.1]** NuGet paketleri yüklendi: `Octokit 14.0.0`, `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4`, `Microsoft.EntityFrameworkCore.Design 9.0.9`, `Hangfire.AspNetCore 1.8.24`, `Hangfire.PostgreSql 1.21.1`, `Swashbuckle.AspNetCore 10.2.3`.
  * **Çözülen Hata:** NuGet varsayılan olarak EF Core 10.x (net10.0) sürümlerini çekiyordu ve `net9.0` ile uyumsuzdu (NU1202). EF paketleri 9.x sürümlerine sabitlendi.
* **28 Temmuz 2026, 13:18** - `docker-compose.yml` yazıldı (postgres:16-alpine, healthcheck'li, kalıcı volume'lü). `appsettings.json` uygulama şemasıyla (GitHub OAuth, JWT, Streak, CORS bölümleri) yeniden yazıldı; sırlar için `appsettings.Development.example.json` şablonu eklendi.
* **28 Temmuz 2026, 13:22** - **[Adım 1.2]** Domain modelleri yazıldı: `Entities/User.cs`, `Entities/Streak.cs`, `Entities/NotificationLog.cs` ve `Enums/NotificationChannel.cs`.
  * CLAUDE.md'deki alan listesine ek olarak `AvatarUrl` (dashboard için), `HasAcceptedTerms` + `TermsAcceptedAt` (KVKK onayının kanıtı) ve `CreatedAt/UpdatedAt` alanları eklendi.
* **28 Temmuz 2026, 13:26** - **[Adım 1.3]** `Data/AppDbContext.cs` yazıldı; `CreatedAt/UpdatedAt` alanlarını merkezi dolduran `SaveChanges` override'ı eklendi. Fluent API konfigürasyonları ayrı dosyalara bölündü: `Data/Configurations/{User,Streak,NotificationLog}Configuration.cs`.
  * Index'ler: `GitHubId` (unique), `GitHubUsername` (unique — rozet endpoint'i için), `IsActive + PreferredNotificationHourUtc` (bildirim job'u için), `UserId + SentAt` (mükerrer bildirim kontrolü için). User↔Streak 1-1, User↔NotificationLog 1-N, ikisi de `Cascade` delete.
* **28 Temmuz 2026, 13:29** - `Program.cs` yapılandırıldı: EF Core + Npgsql (`EnableRetryOnFailure`), Hangfire + PostgreSQL storage, CORS (`FrontendPolicy`), Swagger UI, `/health` endpoint'i ve Development ortamında otomatik migration uygulaması.
* **28 Temmuz 2026, 13:31** - **[Adım 1.3]** `Data/DesignTimeDbContextFactory.cs` eklendi; böylece migration üretmek için uygulamanın tamamının ayağa kalkması gerekmiyor. `InitialCreate` migration'ı üretildi (`Data/Migrations/`).
* **28 Temmuz 2026, 13:33** - PostgreSQL container'ı ayağa kaldırıldı ve migration veritabanına uygulandı. `users`, `streaks`, `notification_logs` + `__EFMigrationsHistory` tabloları doğrulandı.
  * **Çözülen Hata:** 5432 ve 5433 portları makinedeki diğer projelerin container'ları (`sectorscope`, `berkportfolio`) tarafından kullanıldığı için PostgreSQL **5434** portuna alındı.
* **28 Temmuz 2026, 13:36** - **Çözülen Hata (runtime):** `Microsoft.AspNetCore.OpenApi 9.x` (Microsoft.OpenApi 1.x bekliyor) ile `Swashbuckle 10.x` (Microsoft.OpenApi 2.7.5 getiriyor) çakışarak uygulamayı `TypeLoadException` ile düşürüyordu. Kullanılmayan `Microsoft.AspNetCore.OpenApi` paketi kaldırıldı.
* **28 Temmuz 2026, 13:37** - **FAZ 1 TAMAMLANDI ✅** Uçtan uca doğrulandı: `GET /health` → `200 {"status":"healthy","database":"connected"}`, `/swagger` → 200, `/swagger/v1/swagger.json` → 200, `/hangfire` dashboard → 200, Hangfire background server dispatcher'ları sorunsuz başladı.

---

#### 🔹 FAZ 2 — GitHub API Entegrasyonu ve Servis Katmanı

* **28 Temmuz 2026, 13:45** - Kullanıcıya **GitHub OAuth App oluşturma rehberi** aktarıldı (callback URL: `http://localhost:5157/api/v1/auth/github/callback`, istenecek scope'lar: `repo`, `read:user`, `user:email`). `repo` scope'unun neden zorunlu olduğu ve KVKK metninde şeffaf biçimde belirtilmesi gerektiği not edildi.
* **28 Temmuz 2026, 13:48** - `Options/GitHubOptions.cs` eklendi; `GitHub` yapılandırma bölümü Options pattern ile bağlandı.
* **28 Temmuz 2026, 13:52** - **[Adım 2.1]** `Services/Interfaces/IGitHubService.cs` yazıldı. Destek modelleri: `Models/GitHub/ContributionDay.cs`, `Models/GitHub/NotificationRepoSetup.cs`, `Models/GitHub/GraphQl/ContributionsQueryResponse.cs` ve `Exceptions/GitHubServiceException.cs`.
* **28 Temmuz 2026, 13:58** - **[Adım 2.1]** `Services/GitHubService.cs` yazıldı:
  * `GetContributionDaysAsync` — GitHub **GraphQL** `contributionsCollection` sorgusu ile günlük katkı takvimi (private repo katkıları dahil, `repo` scope'u sayesinde).
  * `HasUserCommittedTodayAsync` — bugünün UTC tarihinde katkı var mı.
  * `CreatePrivateNotificationRepoAsync` — gizli repo oluşturma; `RepositoryExistsException` yakalanarak **idempotent** çalışır.
  * `EnsureNotificationIssueExistsAsync` — sabit başlıklı (`🔥 Streak Bildirimleri`) Issue'yu bulur veya oluşturur; Issue gövdesine kullanıcıyı bilgilendiren ve "bu Issue'yu kapatma" uyarısı içeren metin yazılır.
  * `SetUpNotificationInfrastructureAsync` — 1-Click Onboarding için repo + Issue kurulumunu tek adımda yapar.
  * **Resilience:** `WrapGitHubException` ile Octokit istisnaları tek tipe çevrildi; `RateLimitExceededException` ayrı işlenip `IsRateLimited` + `RateLimitResetsAt` bilgisi taşınıyor (Faz 3'te job'ların yeniden deneme kararı bu bilgiye dayanacak). Tüm hatalar `ILogger` ile loglanıyor.
* **28 Temmuz 2026, 14:04** - **[Adım 2.2]** `Services/StreakCalculator.cs` — streak hesaplama mantığı **saf/yan etkisiz** bir sınıfa ayrıldı (DB ve GitHub bağımlılığı yok, doğrudan test edilebilir).
  * **Kritik iş kuralı:** Kullanıcı bugün henüz commit atmadıysa seri **kırılmış sayılmaz** (gün bitmemiştir); seri dünden geriye sayılır. Seri ancak dün de commit yoksa sıfırlanır. Bildirim motorunun tüm anlamı bu kurala dayanıyor.
* **28 Temmuz 2026, 14:08** - **[Adım 2.2]** `Services/Interfaces/IStreakService.cs` + `Services/StreakService.cs` yazıldı. `UpdateUserStreakAsync` son 364 günlük katkı takvimini tek GraphQL çağrısıyla çekip streak'i baştan hesaplar ve DB'ye yazar. `LongestStreak` yalnızca büyüdüğünde güncellenir — 1 yıllık pencere daha eski bir rekoru göremeyeceği için mevcut rekor asla düşürülmez.
* **28 Temmuz 2026, 14:10** - `Program.cs`'e DI kayıtları eklendi: `IGitHubService`, `IStreakService` (Scoped) ve GraphQL çağrıları için named `HttpClient` (30 sn timeout, soket tüketimini önlemek üzere `IHttpClientFactory`).
  * **Çözülen Hata:** `ProductHeaderValue` tipi `System.Net.Http.Headers` ile `Octokit` arasında belirsizdi (CS0104); Octokit'in tipi tam adıyla nitelendi.
* **28 Temmuz 2026, 14:15** - `backend/tests/StreakTracker.Tests` xUnit projesi kuruldu ve `StreakCalculatorTests.cs` yazıldı. **10 senaryo, 10/10 geçti:** boş takvim, bugün dahil kesintisiz seri, bugün commit yokken serinin korunması, dün de commit yokken serinin sıfırlanması, geçmişteki rekorun ayrı bulunması, tek günlük seri, sıfır katkılı günlerin seriyi bölmesi, sırasız/tekrarlı girişler, gelecek tarihli günlerin yok sayılması, ay/yıl sınırının aşılması.
  * **Çözülen Hata:** Test projesinde `MSB3277` — Npgsql 9.0.4'ün getirdiği EF Core 9.0.1 ile API'nin 9.0.9'u çakışıyordu. `Microsoft.EntityFrameworkCore` ve `Microsoft.EntityFrameworkCore.Relational` 9.0.9 olarak explicit eklenip tüm EF sürümleri hizalandı.
* **28 Temmuz 2026, 14:18** - **FAZ 2 TAMAMLANDI ✅** Doğrulama: `dotnet build` → **0 warning / 0 error**, `dotnet test` → **10/10 passed**, API yeni DI kayıtlarıyla ayağa kalktı ve `GET /health` → 200 döndü.
  * ⏳ **Bekleyen canlı test:** GitHub OAuth App kimlik bilgileri girildikten sonra gerçek bir hesapla contribution sorgusu, gizli repo ve Issue oluşturma uçtan uca denenecek.

---

#### 🔹 FAZ 5.1 — GitHub OAuth & Auth Katmanı *(sıra dışı öne alındı)*

> **Not:** Faz 2'nin hiçbir metodu access token olmadan canlı test edilemediği için, Faz 3'e geçmeden önce Faz 5.1 öne alındı. Böylece Faz 2 gerçek verilerle doğrulanabilir hale geldi ve Faz 3'ün job'ları için gerçek kullanıcı kaydı oluşabilecek.

* **28 Temmuz 2026, 14:25** - Kullanıcı GitHub OAuth App'i oluşturdu; `ClientId` ve `ClientSecret` `appsettings.Development.json` dosyasına girildi (dosya `.gitignore`'da).
* **28 Temmuz 2026, 14:28** - `Microsoft.AspNetCore.Authentication.JwtBearer 9.0.9` eklendi. `Options/JwtOptions.cs` yazıldı.
* **28 Temmuz 2026, 14:31** - `IGitHubService.GetAuthenticatedUserAsync` eklendi. Kullanıcı e-postasını gizlemişse `user:email` scope'u ile doğrulanmış birincil adres ayrıca sorgulanır; bu adım başarısız olursa giriş akışı **bloklanmaz**.
* **28 Temmuz 2026, 14:35** - `Services/JwtTokenService.cs` yazıldı. Anahtar 32 bayttan kısaysa servis başlangıçta hata fırlatır (HMAC-SHA256 gereksinimi).
* **28 Temmuz 2026, 14:42** - `Services/AuthService.cs` yazıldı:
  * `BuildAuthorizationUrl` — `repo read:user user:email` scope'ları ve `state` ile GitHub yetkilendirme adresi üretir.
  * `ExchangeCodeForTokenAsync` — geçici `code`'u access token'a çevirir. GitHub hata döndüğünde detay **loglanır ama istemciye sızdırılmaz**.
  * `UpsertUserAsync` — eşleştirme değişmez olan **GitHubId** üzerinden yapılır (kullanıcı adı değişebilir). Yeni kullanıcı `HasAcceptedTerms=false` ile oluşur.
* **28 Temmuz 2026, 14:48** - `Services/OnboardingService.cs` yazıldı. **KVKK kararı:** `AcceptTerms=false` ise işlem reddedilir — onay alınmadan kullanıcının GitHub hesabında hiçbir şey oluşturulmaz. Onay sonrası gizli repo + Issue kurulur, `TermsAcceptedAt` damgalanır ve ilk streak hesaplaması yapılır.
* **28 Temmuz 2026, 14:52** - `Controllers/AuthController.cs` + `Controllers/OnboardingController.cs` + `Controllers/BaseApiController.cs` yazıldı.
  * **CSRF koruması:** `login` adımında rastgele 32 baytlık `state` üretilip `HttpOnly` + `SameSite=Lax` çerezine yazılır; `callback` adımında `CryptographicOperations.FixedTimeEquals` ile sabit zamanlı karşılaştırma yapılır.
  * Kullanıcı GitHub ekranında izni reddederse (`error` parametresi) anlamlı bir 400 döner.
* **28 Temmuz 2026, 14:55** - `Middleware/GlobalExceptionHandler.cs` eklendi. Rate-limit → **429** (sıfırlanma saatiyle), GitHub hataları → **502**, geçersiz istekler → **400**, beklenmeyenler → **500** (iç detay sızdırılmaz). Controller'lar try-catch ile dolmuyor.
* **28 Temmuz 2026, 14:58** - `Program.cs`: JWT authentication (`MapInboundClaims=false` ile `sub` claim'i korunur), `AddAuthorization`, `UseAuthentication`, `UseExceptionHandler` ve Swagger'a **Authorize butonu** eklendi. DI: `IAuthService`, `IOnboardingService`, `IJwtTokenService`.
  * **Çözülen Hata:** Swashbuckle 10.x / Microsoft.OpenApi 2.x'te `AddSecurityRequirement` imzası değişmiş — `Func<OpenApiDocument, OpenApiSecurityRequirement>` alıyor ve sözlük değeri `List<string>` olmuş (CS1950 + CS1503). Lambda ve `List<string>` ile düzeltildi.
* **28 Temmuz 2026, 15:02** - **FAZ 5.1 TAMAMLANDI ✅** Doğrulama: build **0 warning / 0 error**, test **10/10 passed**, kayıtlı endpoint'ler `/api/v1/auth/github/login`, `/api/v1/auth/github/callback`, `/api/v1/auth/me`, `/api/v1/onboarding/complete`, `/health`.
  * `GET /api/v1/auth/github/login` → **302**, `Location` doğru scope'larla GitHub'a gidiyor, `state` çerezi `httponly; samesite=lax` olarak kuruluyor.
  * `GET /api/v1/auth/me` ve `POST /api/v1/onboarding/complete` token'sız → **401**.
* **28 Temmuz 2026, 11:56** - 🎉 **İLK CANLI UÇTAN UCA TEST BAŞARILI** — Gerçek GitHub hesabı (`Berkowhiskey`) ile:
  * `GET /api/v1/auth/github/login` → GitHub izin ekranı → Authorize → callback JWT üretti.
  * `GET /api/v1/auth/me` → **200**, kullanıcı profili döndü.
  * `POST /api/v1/onboarding/complete` → **200**. Gizli repo `.streak-tracker-notifications` ve **Issue #1** GitHub hesabında gerçekten oluşturuldu.
  * **Streak gerçek veriyle hesaplandı:** `CurrentStreak=1`, `LongestStreak=5`, `LastCommitDate=2026-07-28`, `HasCommittedToday=true`.
  * **Idempotency doğrulandı:** Onboarding ikinci kez çağrıldığında `wasAlreadySetUp=true` döndü, ikinci repo/Issue **oluşmadı**.
  * **KVKK koruması doğrulandı:** `acceptTerms=false` ile çağrıldığında **400** — "onay vermeniz gerekir" mesajıyla reddedildi, hesapta hiçbir şey oluşturulmadı.
  * **Sonuç:** Faz 2'nin (GraphQL katkı sorgusu, gizli repo, Issue, streak hesaplama) ve Faz 5.1'in (OAuth, JWT, onboarding) tamamı artık **canlı doğrulanmış** durumda; "bekleyen canlı test" notları kapandı.
  * ⚠️ **Açık risk:** Veritabanında artık gerçek bir GitHub access token'ı **düz metin** olarak duruyor. Faz 5 kapanışında DataProtection ile şifreleme yapılmalı (`User.AccessToken` üzerindeki TODO).

---

#### 🔹 FAZ 3 — Arka Plan Görevleri ve Bildirim Motoru

* **28 Temmuz 2026, 12:20** - `NotificationLog` entity'sine **`IsTest`** alanı eklendi ve `AddIsTestToNotificationLog` migration'ı uygulandı.
  * **Gerekçe:** Mükerrer bildirim kontrolü test kayıtlarını saymamalı; aksi halde kullanıcının gönderdiği bir test bildirimi, o günün gerçek uyarısını engellerdi.
* **28 Temmuz 2026, 12:24** - `IGitHubService.SendNotificationCommentAsync` eklendi — gizli repodaki sabit Issue'ya `@mention`'lı yorum atar. **Projenin temel bildirim hilesi budur:** GitHub Mobile bu yorumu anında push bildirimine çevirir.
* **28 Temmuz 2026, 12:28** - `Services/NotificationMessageBuilder.cs` — mesaj üretimi saf/yan etkisiz bir sınıfa ayrıldı.
  * **Tasarım kararı:** Mesaj her zaman `@kullaniciadi` ile **başlar**; push bildiriminde metnin yalnızca baş kısmı göründüğü için uyarının kilit bilgisi öne alındı.
  * Serisi olmayan kullanıcıya "0 günlük serin var" denmez; rekoru yoksa rekor satırı gösterilmez.
* **28 Temmuz 2026, 12:33** - **[Adım 3.2]** `Services/NotificationService.cs` yazıldı:
  * `ProcessHourlyNotificationsAsync` — o saate ayarlı, aktif, onay vermiş ve altyapısı kurulu kullanıcıları işler. **Hata izolasyonu:** bir kullanıcıda oluşan hata (geçersiz token, rate-limit, silinmiş repo) turun geri kalanını durdurmaz.
  * Karar vermeden önce streak GitHub'dan **tazelenir** — kullanıcı bildirim saatinden hemen önce commit atmış olabilir.
  * `HasBeenNotifiedTodayAsync` — aynı gün mükerrer bildirim engellenir (test kayıtları hariç).
  * Başarısız denemeler de `NotificationLog`'a yazılır; sorunlar geriye dönük incelenebilir.
* **28 Temmuz 2026, 12:36** - **[Adım 3.1]** `Jobs/StreakCheckJob.cs` yazıldı ve `Program.cs`'te Hangfire recurring job olarak kaydedildi (`0 * * * *`, UTC). Her saat başı çalışıp o saati seçmiş kullanıcıları işler; Hangfire dashboard'ından elle de tetiklenebilir.
* **28 Temmuz 2026, 12:38** - `Controllers/NotificationsController.cs` eklendi: `POST /api/v1/notifications/test` (kurulum doğrulama) ve `POST /api/v1/notifications/check-now` (zamanlanmış turu beklemeden gerçek uyarı değerlendirmesi).
* **28 Temmuz 2026, 12:38** - **FAZ 3 TAMAMLANDI ✅ — CANLI DOĞRULANDI**
  * Build **0 warning / 0 error**, test **18/18 passed** (10 streak + 8 bildirim mesajı).
  * 📱 `POST /api/v1/notifications/test` → **200**, `sent: true`. Gerçek GitHub hesabındaki Issue'ya yorum düştü ve **telefona push bildirimi gitti**.
  * `POST /api/v1/notifications/check-now` → `sent: false`, *"Bugun commit atilmis, serin guvende."* — koruma mantığı doğru çalışıyor.
  * `notification_logs` tablosunda kayıt doğrulandı: `GitHubIssue | IsSuccess=t | IsTest=t`.

---

#### 🔹 FAZ 4 — Dinamik SVG Rozet (Badge) Servisi

* **28 Temmuz 2026, 12:50** - Git güvenlik denetimi yapıldı (kullanıcı commit/push öncesi sordu): `appsettings.Development.json` ignore ediliyor, takip edilecek 57 dosyanın **hiçbirinde** gerçek `ClientId`/`ClientSecret` yok. Tarama deseninin çalıştığı, ignore'lu dosyada eşleşme bulunarak ayrıca doğrulandı.
* **28 Temmuz 2026, 12:55** - `Models/Badges/BadgeData.cs` yazıldı: rozet verisi (`BadgeData`), tema (`BadgeTheme`) ve renk paleti (`BadgePalette`). Dark/Light için GitHub'ın kendi renk değerleri baz alındı.
* **28 Temmuz 2026, 13:02** - **[Adım 4.1]** `Services/SvgBadgeService.cs` yazıldı:
  * **Vektörel alev** çizildi — emoji kullanılmadı. Gerekçe: GitHub rozetleri `<img>` olarak işler ve emoji render'ı platforma göre değişir/bozulur; SVG path her yerde aynı görünür.
  * **Font:** yalnızca işletim sistemlerinde hazır bulunan font yığını kullanıldı (harici font yüklenemez).
  * **Durum yansıtma:** serisi olmayan kullanıcıda alev sönük (gri) çizilir; serisi olup bugün commit atmamışsa alev soluklaşır (`opacity 0.65`).
  * **Güvenlik:** kullanıcı adı URL'den geldiği için `SecurityElement.Escape` ile XML kaçışı yapılır (içerik enjeksiyonu koruması).
  * `GenerateNotFoundBadge` — README'de kırık resim yerine bilgilendirici rozet.
  * `ComputeETag` — görünümü etkileyen tüm alanların SHA256 imzası.
* **28 Temmuz 2026, 13:06** - `IStreakService.GetBadgeDataAsync` eklendi. **Tek sorguda** veritabanından okur, GitHub API'sine **hiç gitmez** — CLAUDE.md'deki "%100 uptime, milisaniyelerde render" hedefinin teknik karşılığı budur. Kullanıcı adı büyük/küçük harf duyarsız eşleşir.
* **28 Temmuz 2026, 13:10** - **[Adım 4.2]** `Controllers/BadgeController.cs` yazıldı: `GET /api/v1/badges/{username}.svg?theme=dark|light`, `image/svg+xml`, `ETag` + `Cache-Control: public, max-age=300`, `If-None-Match` ile **304** desteği. Kayıtsız kullanıcı için `no-cache` (kullanıcı birazdan kaydolabilir).
* **28 Temmuz 2026, 13:14** - `SvgBadgeServiceTests.cs` yazıldı — **11 yeni test**. Kritik olan: üretilen SVG'nin `XDocument.Parse` ile **geçerli XML** olduğu doğrulanıyor; bozuk XML README'de kırık resim demektir.
* **28 Temmuz 2026, 13:16** - **FAZ 4 TAMAMLANDI ✅ — CANLI DOĞRULANDI**
  * Build **0 warning / 0 error**, test **29/29 passed**.
  * `GET /badges/Berkowhiskey.svg` → **200**, `Content-Type: image/svg+xml`, `ETag: "e17d32e11494761b"`, `Cache-Control: public, max-age=300`.
  * Aynı ETag ile tekrar istek → **304 Not Modified** ✅
  * Küçük harfli adres (`berkowhiskey.svg`) → **200** ✅
  * Kayıtsız kullanıcı → **200** + bilgilendirici rozet + `no-cache` ✅
  * ⏳ **Bekleyen:** Rozet şu an yalnızca `localhost` üzerinden erişilebilir; profil README'sine eklenebilmesi için canlı bir adrese deploy gerekiyor.

