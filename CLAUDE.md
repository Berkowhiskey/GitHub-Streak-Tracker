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

---

#### 🔹 FAZ 5.2 — Güvenlik Borçlarının Kapatılması + Kullanıcı/Streak Endpoint'leri

* **29 Temmuz 2026, 08:55** - Kullanıcı Faz 1-4'ü commit'leyip `github.com/Berkowhiskey/GitHub-Streak-Tracker` reposuna push etti. Push sonrası güvenlik denetimi: push edilen commit içeriğinde gerçek `ClientId`/`ClientSecret` **yok**, `appsettings.Development.json` takip edilmiyor ✅
* **29 Temmuz 2026, 09:02** - 🔐 **AÇIK RİSK KAPATILDI — Access token şifrelemesi.** `Services/TokenProtector.cs` (ASP.NET DataProtection) yazıldı ve `AppDbContext`'te **EF value converter** olarak bağlandı.
  * **Neden value converter:** Şifreleme model seviyesinde tanımlandığı için hiçbir serviste unutulamaz; token DB'ye her zaman şifreli yazılır, okunurken çözülür.
  * `AccessToken` kolonu 500 → **1000** karaktere çıkarıldı (DataProtection zarfı + base64 uzunluğu). `EncryptAccessToken` migration'ı uygulandı.
  * DataProtection anahtarları `.dataprotection-keys/` klasörüne kalıcı yazılıyor ve `.gitignore`'a eklendi — **anahtarlar sırdır**, sızarsa kayıtlı token'lar çözülebilir.
  * `DesignTimeDbContextFactory` geçici bir koruyucu üretecek şekilde güncellendi; böylece migration üretimi bozulmadı.
* **29 Temmuz 2026, 09:14** - **Yarım kalan işin tamamlanması:** Şifreleme altyapısı kurulmuştu ama veritabanındaki mevcut token hâlâ **düz metindi** (`gho_`, 40 karakter) — yalnızca fallback sayesinde çalışıyordu. `Data/TokenEncryptionBackfill.cs` yazıldı: uygulama açılışında ham SQL ile şifrelenmemiş token'ları tespit edip yeniden yazar. İdempotenttir.
  * **Doğrulama:** Backfill logu `1 adet duz metin access token sifrelendi`. DB'de token artık **176 karakter, `CfDJ` önekli** (DataProtection formatı). Şifrelemeden **sonra** `POST /streaks/me/refresh` → **200** — yani çözme de sorunsuz çalışıyor.
* **29 Temmuz 2026, 09:06** - 🔐 **AÇIK RİSK KAPATILDI — Hangfire dashboard.** `Middleware/HangfireDashboardAuthorizationFilter.cs` eklendi. Development'ta serbest, Production'da yalnızca loopback/sunucu IP'sinden erişilebilir. Dashboard artık Production'da da kayıtlı ama filtre arkasında.
* **29 Temmuz 2026, 09:10** - **[Adım 5.2]** `Controllers/UsersController.cs` yazıldı:
  * `GET /api/v1/users/me` — profil özeti.
  * `PATCH /api/v1/users/me/preferences` — bildirim saati (0-23 doğrulamalı) ve bildirimlerin açık/kapalı durumu.
  * `GET /api/v1/users/me/badge` — README'ye yapıştırılacak hazır **Markdown ve HTML** rozet kodları (`App:PublicBaseUrl` üzerinden üretilir).
  * `DELETE /api/v1/users/me` — **KVKK silme hakkı.** Kullanıcı ve tüm verileri (streak + bildirim logları, cascade) silinir. GitHub'daki gizli repo **silinmez**; ona yalnızca kullanıcı karar verebilir, yanıtta bu açıkça bildirilir.
* **29 Temmuz 2026, 09:12** - **[Adım 5.2]** `Controllers/StreaksController.cs` yazıldı: `GET /streaks/me` (DB'den, hızlı), `POST /streaks/me/refresh` (GitHub'dan tazeler), `GET /streaks/me/calendar?days=364` (Faz 6 heatmap'i için günlük katkı verisi).
* **29 Temmuz 2026, 09:20** - **FAZ 5.2 TAMAMLANDI ✅ — CANLI DOĞRULANDI**
  * Build **0 warning / 0 error**, test **29/29 passed**.
  * `GET /users/me` → 200 · `GET /users/me/badge` → 200 (Markdown + HTML kodları) · `GET /streaks/me` → 200 · `POST /streaks/me/refresh` → 200 · `GET /streaks/me/calendar?days=7` → 200 (8 günlük gerçek katkı verisi) · `PATCH /users/me/preferences` → 200 · geçersiz saat (99) → **400**.
  * **Streak gerçek veriyle güncellendi:** Kullanıcının commit/push'u sonrası `CurrentStreak=1 → 2`, `LastCommitDate=2026-07-29`.
  * **Test yan etkisi temizlendi:** Test sırasında 21'e alınan bildirim saati 20'ye geri alındı.
  * ⚠️ **Kalan not:** `docker-compose.yml` içindeki geliştirme veritabanı şifresi hâlâ açık metin. Canlıya çıkarken environment variable'a taşınmalı.

---

#### 🔹 FAZ 6 — Frontend Arayüzü (Next.js + Tailwind + shadcn/ui)

* **29 Temmuz 2026, 09:30** - **Backend'de tarayıcı akışı için düzenleme:** `AuthController.Callback` artık JSON yerine **frontend'e yönlendirme** yapıyor ve JWT'yi `HttpOnly` çereze yazıyor.
  * **Güvenlik kararı:** Token `localStorage` yerine `HttpOnly` çerezde tutuluyor — JavaScript okuyamadığı için XSS ile çalınamaz.
  * `JwtBearerEvents.OnMessageReceived` ile çerezden token okunuyor; `Authorization` başlığı varsa ona dokunulmuyor, **Swagger ve curl çalışmaya devam ediyor**.
  * Hata durumlarında da frontend'e anlamlı `?error=` parametresiyle dönülüyor. `POST /api/v1/auth/logout` eklendi.
* **29 Temmuz 2026, 09:35** - **[Adım 6.1]** `frontend/` oluşturuldu: **Next.js 16.2.12**, React 19.2, TypeScript, Tailwind v4, App Router. `shadcn/ui` kuruldu (button, card, label, switch, input, select, separator, skeleton). `layout.tsx`'te `lang="tr"` ve **dark mode varsayılan**.
  * ⚠️ **Kritik tespit:** `create-next-app`, `frontend/AGENTS.md` ile *"Bu bildiğin Next.js değil, `node_modules/next/dist/docs/` içindeki rehberi oku"* uyarısı bırakmış. Dokümanlar okundu ve iki breaking change'e göre kod yazıldı: **`params`/`searchParams` artık `Promise`** (await zorunlu) ve **Turbopack varsayılan**.
  * **Çözülen Hata:** shadcn'in yeni sürümü Radix yerine **Base UI** kullanıyor; `Button` bileşeninde `asChild` prop'u yok (TS2322). Landing'deki giriş bağlantısı `buttonVariants()` sınıflarıyla `<a>` olarak yazıldı — prop API'sine bağımlılık ortadan kalktı.
* **29 Temmuz 2026, 09:40** - `lib/api.ts` yazıldı: backend DTO'larıyla birebir eşleşen TypeScript tipleri ve tüm endpoint'leri saran istemci. Her istek `credentials: "include"` gönderir; `ApiError` sınıfı backend'in `ProblemDetails` yanıtlarını taşır ve 401'i ayırt eder.
* **29 Temmuz 2026, 09:44** - **[Adım 6.2]** `app/page.tsx` (landing) yazıldı — Server Component, `await searchParams` ile callback hataları kullanıcıya Türkçe gösteriliyor. `app/gizlilik/page.tsx` eklendi: hangi iznin **neden** istendiği, nelerin saklandığı/saklanmadığı ve silme hakkı açıkça yazıldı.
* **29 Temmuz 2026, 09:48** - **[Adım 6.2]** `app/onboarding/page.tsx` yazıldı. **KVKK yaklaşımı:** Onay kutusu işaretlenmeden buton pasif; ne yapılacağı üç madde hâlinde açıkça anlatılıyor (gizli repo, Issue, katkı takvimi okuma) ve *"kodlarını okumuyoruz"* ayrıca belirtiliyor. `repo` izninin neden zorunlu olduğu ayrı bir uyarı kutusunda açıklanıyor.
* **29 Temmuz 2026, 09:52** - **[Adım 6.3]** `app/dashboard/page.tsx` ve bileşenleri yazıldı:
  * Streak kartları (güncel seri, rekor, bugünkü durum) — seri yoksa alev sönük çizilir.
  * `components/contribution-heatmap.tsx` — GitHub tarzı katkı takvimi; geniş içerik kendi içinde yatay kayar, sayfa gövdesi kaymaz.
  * `components/copy-field.tsx` — rozet Markdown/HTML kodları için kopyalama alanı.
  * Bildirim ayarları: saat seçimi (UTC + yerel saat karşılığı gösterilir), bildirimleri aç/kapat, **test bildirimi gönder**.
  * **KVKK silme hakkı:** onay soran "Hesabımı ve verilerimi sil" bölümü; GitHub'daki gizli reponun silinmeyeceği açıkça belirtiliyor.
* **29 Temmuz 2026, 09:57** - **FAZ 6 TAMAMLANDI ✅ — CANLI DOĞRULANDI**
  * Backend build **0 warning / 0 error**, test **29/29 passed**.
  * Frontend `npx tsc --noEmit` → **hatasız**, `npm run build` → **başarılı** (4 sayfa üretildi).
  * Backend + frontend birlikte çalıştırıldı: `GET localhost:3000/` → **200** (içerik doğrulandı), `?error=access_denied` → Türkçe hata mesajı görünüyor, `/gizlilik` → **200**, backend login → **302** doğru scope'larla GitHub'a gidiyor.
  * ⏳ **Bekleyen:** Kullanıcının tarayıcıdan uçtan uca akışı denemesi (giriş → onboarding → dashboard). Mevcut oturum JSON tabanlıydı; çerez akışı için yeniden giriş gerekiyor.

---

#### 🔹 FAZ 7 — Kritik Bildirim Hatasının Düzeltilmesi (GitHub App'e Geçiş) + Tipografi

* **29 Temmuz 2026, 10:35** - 🐞 **KRİTİK HATA TESPİT EDİLDİ.** Kullanıcı, test bildiriminin Issue'ya düştüğünü ama **telefona push gelmediğini** bildirdi.
  * **Kök neden (araştırmayla doğrulandı):** GitHub, **kullanıcının kendi yaptığı eylemler için ona bildirim göndermez** — kendi kendini `@mention` etse bile. Kapatılabilir bir ayarı da yok.
  * Bizim sistemde yorum, **kullanıcının kendi access token'ıyla, kendi adına** atılıyordu. Dolayısıyla GitHub bunu "kendi yorumun" sayıp bildirim üretmiyordu. Yorum Issue'da görünüyor ama push doğmuyordu.
  * **Bu, projenin temel hilesini işlevsiz bırakan bir tasarım hatasıydı.** Kullanıcının "Inbox'a düşürsek olur mu?" sorusu doğru yöndeydi ama sorun *nereye* değil, **kim olarak** yazdığımızdı.
* **29 Temmuz 2026, 10:42** - **Çözüm kararı: GitHub App'e geçiş.** Alternatif olan "bot hesabı + otomatik collaborator" yöntemi de değerlendirildi; GitHub App seçildi çünkü izinler **yalnızca bildirim reposuna** sınırlanabiliyor ve bu, uzun süredir açık olan geniş `repo` scope'u endişesini de hafifletiyor.
* **29 Temmuz 2026, 10:50** - `Options/GitHubAppOptions.cs` yazıldı. Private key hem doğrudan içerik hem de **dosya yolu** (`PrivateKeyPath`) olarak verilebiliyor — PEM'i JSON'a taşımak zahmetli olduğu için. `.gitignore`'a `*.pem` eklendi.
* **29 Temmuz 2026, 10:56** - `Services/GitHubAppService.cs` yazıldı:
  * `GenerateAppJwt` — App private key'i ile **RS256** imzalı, 9 dakikalık JWT. Saat kaymalarına karşı `notBefore` 60 saniye geriden başlatılıyor (GitHub önerisi). `CacheSignatureProviders=false` ile RSA nesnesi serbest bırakıldıktan sonra yeniden kullanım hatası engellendi.
  * `GetInstallationIdAsync` — kullanıcı App'i kurmuşsa kurulum kimliği; kurmamışsa `null` (hata değil, beklenen durum).
  * `SendNotificationCommentAsync` — installation token ile yorum **`streaktracker[bot]` kimliğiyle** atılır. Bildirimin doğmasını sağlayan şey budur.
* **29 Temmuz 2026, 11:00** - `NotificationService` bot üzerinden gönderecek şekilde güncellendi. `IGitHubService.SendNotificationCommentAsync` **kaldırıldı** — artık bildirim üretmediği için tutulması yanıltıcı olurdu.
  * **Dürüstlük kararı:** App kurulu değilse "gönderildi" denmiyor; `sent:false` ve *"GitHub App'i kurman gerekiyor"* mesajı dönüyor. Sessizce çalışmayan bir bildirim göndermektense açıkça söylemek tercih edildi.
  * `User.GitHubAppInstallationId` alanı eklendi (önbellek) ve `AddGitHubAppInstallationId` migration'ı uygulandı.
* **29 Temmuz 2026, 11:04** - `GET /api/v1/users/me/app-status` eklendi (kurulumu GitHub'a sorar, DB'yi tazeler). Frontend'e `components/app-install-notice.tsx` eklendi: neden gerektiğini açıklayan uyarı + "GitHub App'i kur" + "Kurdum, kontrol et" akışı.
* **29 Temmuz 2026, 10:20** - **Tipografi:** Kullanıcı font tercihini bildirdi; Geist yerine **Inter** (arayüz) + **JetBrains Mono** (kod alanları) kullanılmaya başlandı. Türkçe karakterler için `latin-ext` alt kümesi dahil edildi.
* **29 Temmuz 2026, 11:09** - **Kod tarafı tamamlandı:** backend build **0 warning / 0 error**, test **29/29 passed**, frontend build **başarılı**.
* **29 Temmuz 2026, 12:50** - Kullanıcı GitHub App'i oluşturdu (`AppId: 4422641`, slug `streaktracker-dev`) ancak kurulum adımında takıldı. Ortam incelendi ve **iki hata tespit edildi — ikisi de asistan kaynaklıydı:**
  * **Hata 1 (yanlış varsayım):** `.pem` dosyasının adı tahmin edilmişti (`streaktracker.pem`), oysa GitHub dosyayı tarihli adla indiriyor (`streaktracker-dev.2026-07-29.private-key.pem`). `PrivateKeyPath` gerçek dosya adıyla düzeltildi.
  * **Teşhis notu:** `dotnet run --no-build` çalıştırıldığında `bin/` altındaki **eski** `appsettings.Development.json` kopyası okunuyordu; ayrıca önceki test sürecinde kalan bir `StreakTracker.API` süreci 5157 portunu tutuyor ve eski yapılandırmayla yanıt veriyordu. Süreç sonlandırılıp build alınarak doğrulama yapıldı.
  * **Hata 2 (kod hatası):** GitHub App JWT'si `iat` (issued at) claim'i olmadan üretiliyordu; GitHub bunu zorunlu tutuyor ve `401 - Missing 'issued at' claim ('iat') in assertion` dönüyordu. `JwtSecurityToken` bu claim'i kendiliğinden eklemiyor — `EpochTime.GetIntDate` ile elle eklendi.
* **29 Temmuz 2026, 13:16** - ✅ **GITHUB APP ENTEGRASYONU CANLI DOĞRULANDI**
  * `GET /users/me/app-status` → `{"installed":true,"appConfigured":true}`
  * `POST /notifications/test` → `{"sent":true}`
  * Log: *"Bildirim yorumu **bot kimligiyle** gonderildi. Kullanici: Berkowhiskey, Issue: #1"*
  * `users.GitHubAppInstallationId = 149805682` önbelleğe alındı; sonraki bildirimlerde GitHub'a kurulum sorgusu yapılmayacak.
* **29 Temmuz 2026, 13:25** - 🎉 **PUSH BİLDİRİMİ TELEFONA DÜŞTÜ — PROJENİN TEMEL HİLESİ DOĞRULANDI**
  * Kullanıcı teyit etti: bildirim GitHub Mobile üzerinden telefona push olarak ulaştı.
  * Ayrıca **arayüzden** (dashboard → "Test gönder") gönderilen bildirim de başarıyla düştü; frontend → backend → GitHub App → GitHub Mobile zinciri uçtan uca çalışıyor.
  * **Sonuç:** GitHub App'e geçiş kararı doğruydu. Kullanıcının kendi kimliğiyle atılan yorum bildirim üretmiyordu; bot kimliğiyle atılan yorum üretiyor. Faz 7 kapandı ✅

---

#### 🔹 FAZ 8 — Deploy Öncesi Kritik Hazırlıklar (Aşama 0)

> **Hedef altyapı:** Backend + PostgreSQL → **Oracle Cloud Always Free** (ARM, süresiz $0) · Frontend → **Vercel** ($0) · Reverse proxy + SSL → **Caddy** (otomatik sertifika). Öğrenci bütçesi gözetilerek toplam **aylık $0** hedeflendi. Aşama 0'ın tamamı platformdan bağımsızdır; Oracle hesabı açılmasa bile geçerlidir.

* **29 Temmuz 2026, 13:50** - **[0.1]** DataProtection anahtar klasörü yapılandırılabilir yapıldı (`App:DataProtectionKeysPath`). Container'da kalıcı volume'e (`/keys`) bağlanacak.
  * **Neden DB değil:** `AppDbContext` zaten `ITokenProtector`'a, o da `IDataProtectionProvider`'a bağımlı. Anahtar deposunu aynı DbContext'e bağlamak **döngüsel bağımlılık** riski taşıyor. Tek sunucuda volume hem basit hem güvenli.
* **29 Temmuz 2026, 13:55** - **[0.2]** `UseForwardedHeaders` pipeline'ın en başına eklendi (`KnownNetworks`/`KnownProxies` temizlenerek).
  * **Kritik gerekçe:** Caddy arkasında uygulama HTTP konuşur; bu middleware olmadan `Request.IsHttps` **false** döner → çerez `Secure` işaretlenmez → tarayıcı `SameSite=None` çerezini reddeder → **giriş hiç çalışmaz.** Sessizce başarısız olan türden bir hata.
* **29 Temmuz 2026, 14:00** - **[0.3]** Çerez politikası yapılandırılabilir hale getirildi (`App:CookieSameSite`). `SetAuthCookie` ve `Logout` artık **aynı** ayarları üreten `BuildAuthCookieOptions` üzerinden çalışıyor — farklı ayarlarla silinen çerez tarayıcıda eşleşmez ve oturum kapanmazdı. Development'ta `None` seçilirse uygulama anlamlı hatayla başlamayı reddediyor.
* **29 Temmuz 2026, 14:05** - **[0.4]** `App:RunMigrationsOnStartup` eklendi; `TokenEncryptionBackfill` migration ile **aynı bloğa** ve ondan sonraya alındı (şemaya bağımlı olduğu için).
* **29 Temmuz 2026, 14:10** - **[0.5]** `docker-compose.yml` şifresi `${POSTGRES_PASSWORD}` ile env'e taşındı. `.env.example` şablonu yazıldı; `.gitignore`'a `secrets/` ve `caddy-data/` eklendi.
* **29 Temmuz 2026, 14:15** - **[0.6]** `backend/Dockerfile` (multi-stage, **root olmayan kullanıcı**), `backend/.dockerignore`, `docker-compose.prod.yml` (api + postgres + caddy, adlandırılmış volume'ler) ve `Caddyfile` (otomatik Let's Encrypt + güvenlik başlıkları) yazıldı.
  * **Çözülen Hata:** İlk Docker build'i `MSB4018 / ResolvePackageAssets` ile patladı — Windows'ta üretilen `bin/`+`obj/` klasörleri imaja kopyalanıp Linux restore'unu bozuyordu. `.dockerignore` ile çözüldü.
  * **Neden Caddy:** nginx+certbot sertifika yenileme, webroot ve cron ayarı ister; Caddy sertifikayı kendiliğinden alıp yeniler. VPS bakım yükünü belirgin azaltır.
* **29 Temmuz 2026, 14:25** - 🐞 **CİDDİ HATA BULUNDU VE DÜZELTİLDİ — `TokenEncryptionBackfill` veri bozuyordu.**
  * **Nasıl ortaya çıktı:** Production imajı, host'taki geliştirme veritabanına bağlanarak test edildi. Container'ın kendi (boş) anahtar volume'ü vardı; host anahtarıyla şifrelenmiş token'ı çözemedi.
  * **Kusur:** Backfill "çözülemiyorsa **düz metindir**" varsayıyordu. Oysa değer **başka bir DataProtection anahtarıyla şifrelenmiş** de olabilir. Sonuç: token'ın üzerine ikinci kez şifreleme uygulandı (176 → **368 karakter**) ve **geri dönülmez şekilde bozuldu**.
  * **Etki:** Bu kusur production'da da tetiklenebilirdi — anahtar klasörü yanlış bağlanırsa tüm kullanıcıların token'ları bozulurdu.
  * **Düzeltme:** Artık yalnızca bilinen GitHub token öneklerine (`gho_`, `ghp_`, `ghu_`, `ghs_`, `ghr_`, `github_pat_`) sahip değerler düz metin kabul ediliyor. Çözülemeyen ama bu öneklere sahip olmayan değerlere **dokunulmuyor**; bunun yerine yapılandırma kontrolünü isteyen açık bir `LogError` yazılıyor.
  * **Doğrulama:** Aynı senaryo yeni kodla tekrarlandı → token uzunluğu **368'de kaldı (değişmedi)**, beklenen hata mesajı loglandı ✅
  * ⚠️ **Yan etki:** Kullanıcının mevcut access token'ı bu testte bozuldu; yeniden giriş yapması gerekiyor (veri kaybı yok, yalnızca oturum).
* **29 Temmuz 2026, 14:30** - **AŞAMA 0 TAMAMLANDI ✅**
  * `dotnet build` → **0 warning / 0 error**, `dotnet test` → **29/29 passed**
  * `docker build` → imaj başarıyla üretildi
  * Production modunda container: `/health` → **200 healthy**, `/swagger` → **404** (production'da kapalı ✓), rozet endpoint'i → **200**, migration otomatik uygulandı, DataProtection anahtarı `/keys` volume'üne yazıldı ✓
  * `docker-compose.prod.yml` → sözdizimi ve tüm değişkenler doğrulandı (3 servis, 4 volume)

---

#### 🔹 FAZ 8 — YAYINA ALMA (Aşama 1-3) 🚀

> **Nihai altyapı:** Backend → **Oracle Cloud Always Free** (AMD `E2.1.Micro`, Ubuntu 24.04) · Veritabanı → **Supabase** · Frontend → **Vercel** · SSL/proxy → **Caddy** · Alan adı → **streak-tracker.me** (Namecheap, GitHub Student Pack). **Toplam maliyet: aylık $0.**

* **30 Temmuz 2026, 09:40** - **Oracle Cloud hesabı açıldı** (Home region: Zurich).
  * ⚠️ **Asistan kaynaklı yanlış öneri:** Kapasite bulma şansı yüksek olur diye Zurich önerilmişti. Gerçek tersi çıktı: küçük bölgelerin ARM havuzu küçük **ve tek availability domain** veriyorlar. Frankfurt/Amsterdam'da 3 AD olsaydı deneme şansı üçe çıkardı. Home region sonradan değiştirilemediği için bu geri alınamadı.
  * `VM.Standard.A1.Flex` (ARM) → **"Out of capacity"**. Alternatif AD yok (tek AD).
* **30 Temmuz 2026, 09:55** - **Çözüm: AMD `VM.Standard.E2.1.Micro`.** Araştırmayla doğrulandı: bu shape de Always Free ve ARM'in aksine neredeyse her zaman müsait. Karşılığı: 1 OCPU / **1 GB RAM** (ARM'de 12 GB olacaktı).
* **30 Temmuz 2026, 10:05** - **Kullanıcının Supabase önerisi değerlendirildi.** Önemli ayrım netleştirildi: Supabase bir **veritabanı** servisidir, uygulama barındıramaz (Edge Functions Deno'dur, .NET değil) — yani Oracle'ın yerine geçmez, *bir parçasının* yerine geçer.
  * **Ama kritik bir kazanç sağladı:** Veritabanı Supabase'e taşındığında sunucudaki bellek ihtiyacı ~490 MB'dan ~340 MB'a düştü ve **1 GB RAM kısıtı sorun olmaktan çıktı.**
  * **Neden Supabase, Neon değil:** Neon'un ücretsiz katmanında **100 CU-hour/ay** compute limiti var; Hangfire 15 saniyede bir sorgu attığı için bu limit aşılırdı. Supabase'de compute limiti yok, yalnızca 7 günlük inaktivite pause'u var — Hangfire sürekli sorgu attığı için o durum hiç oluşmuyor.
* **30 Temmuz 2026, 10:20** - Sunucu hazırlandı: **2 GB swap** (`swappiness=10`, 1 GB RAM için güvenlik ağı), **Docker 29.6.2 + Compose v5.3.1**.
* **30 Temmuz 2026, 10:30** - 🔥 **Firewall'un iki katmanı açıldı** — Oracle'da en sık takılan yer:
  1. **Sunucu içi `iptables`:** Oracle Ubuntu imajı `INPUT` zincirinde 22 dışında her şeyi REJECT ediyor. 80/443 kuralları **REJECT satırından ÖNCE** (pozisyon 5-6) eklendi ve `netfilter-persistent` ile kalıcılaştırıldı. Yanlış sıraya eklenirse kurallar sessizce etkisiz kalır.
  2. **Oracle Security List:** Konsoldan 80/443 ingress kuralları (kullanıcı tarafından).
* **30 Temmuz 2026, 10:45** - Repo sunucuya klonlandı; `.env` (600 izinli) ve `secrets/` (700) oluşturuldu. **JWT anahtarı sunucuda `openssl` ile üretildi** — hiçbir yerde dolaşmadı. GitHub App private key'i `scp` ile yüklendi.
* **30 Temmuz 2026, 11:00** - `docker-compose.prod.yml` **Supabase'e göre yeniden yazıldı:** yerel `postgres` servisi kaldırıldı, bağlantı Supavisor'a yönlendirildi.
  * **Supavisor session mode (port 5432) kullanıldı**, transaction mode (6543) değil — transaction mode prepared statement desteklemez ve EF Core ile Hangfire'ın ikisi de buna ihtiyaç duyar.
  * **Pooler adresi zorunlu:** Supabase'in doğrudan bağlantısı yalnızca IPv6 üzerinden erişilebilir; sunucumuz IPv4.
  * `Maximum Pool Size=12` ile sınırlandı (Npgsql varsayılanı 100) — ücretsiz katmanın bağlantı bütçesini tüketmemek için.
* **30 Temmuz 2026, 11:06** - 🎉 **BACKEND YAYINDA.** `docker compose up -d --build` → 1 OCPU'da build tamamlandı, migration'lar Supabase'e uygulandı, Caddy **Let's Encrypt sertifikasını otomatik aldı**.
  * `GET https://api.streak-tracker.me/health` → **200** `{"status":"healthy","database":"connected"}`
  * `/swagger` → **404** (production'da kapalı ✓) · rozet → **200** · korumalı endpoint token'sız → **401** · OAuth login → **302** doğru production callback'iyle
  * SSL: Let's Encrypt, 28 Ekim 2026'ya kadar, otomatik yenilenecek
* **30 Temmuz 2026, 11:07** - 🛡️ **Gerçek dünya gözlemi:** Sunucu yayına çıktığı dakikalar içinde bir zafiyet tarayıcı botu (`143.198.85.89`) `/backup/.env`, `/actuator/env`, `/config/default.json` gibi yolları taramaya başladı. **Hepsi 404** döndü — Caddy hiç statik dosya servis etmiyor, yalnızca API'ye reverse proxy yapıyor. Aşama 0'da sırları env'e taşıma kararı ilk dakikadan işe yaradı.
* **30 Temmuz 2026, 14:50** - **Frontend Vercel'e alındı** (root directory `frontend`, `NEXT_PUBLIC_API_BASE_URL=https://api.streak-tracker.me`).
  * **Çözülen yapılandırma hatası:** Vercel apex'i otomatik olarak `www`'ye 308 ile yönlendiriyordu. Bu bırakılsaydı tarayıcı API'ye `Origin: https://www.streak-tracker.me` gönderecek, CORS ayarımız apex beklediği için **dashboard hiç veri çekemeyecekti**. Yön ters çevrildi: apex birincil (Production), `www` → apex.
  * Ayrıca `www` varyantı da izin verilen origin listesine **güvenlik ağı** olarak eklendi (`Cors__AllowedOrigins__1`).
  * **Çözülen DNS çakışması:** `www` için iki CNAME kaydı vardı (biri apex'e, biri Vercel'e). Aynı host için tek CNAME olabilir; eskisi kaldırıldı.
* **30 Temmuz 2026, 15:10** - ✅ **FRONTEND YAYINDA.** `https://streak-tracker.me` → **200**, içerik ve API adresi doğrulandı, SSL `CN=streak-tracker.me`.
  * ℹ️ Kullanıcının bilgisayarında eski GitHub Pages IP'leri cache'te kaldığı için sayfa geç göründü; **telefondan doğrulandı, altyapı sağlam.** Sebep: Wi-Fi ağ geçidinin (`172.20.10.1`) DNS cache'i. Bir süre sonra kendiliğinden düzeldi.
* **30 Temmuz 2026, 15:45** - 🐞 **Çözülen Hata (deploy kaynaklı): "GitHub App yapılandırılmamış" uyarısı.** Panelde App kurulu olmasına rağmen bu mesaj görünüyordu.
  * **Kök neden:** Dockerfile güvenlik gereği **root olmayan kullanıcı** (uid 5678) kullanıyor, ancak host'taki `secrets/` klasörü `ubuntu` kullanıcısına (uid 1001) ait ve `700` izinliydi. Bind mount host izinlerini koruduğu için container klasöre **hiç giremiyordu** (`Permission denied`).
  * `GitHubAppOptions.IsConfigured`, `File.Exists(PrivateKeyPath)` kontrolüne dayanıyor; .NET izin hatasında istisna fırlatmaz, `false` döner. Bu yüzden hata "yapılandırma eksik" gibi göründü — **sessizce yanlış yönlendiren** bir durum.
  * **Düzeltme:** `sudo chown -R 5678:5678 secrets` + `chmod 700/400`. Böylece anahtarı yalnızca uygulama okuyabiliyor; sunucuya SSH ile giren biri bile `sudo` olmadan göremiyor.
  * Tekrar yaşanmaması için `docker-compose.prod.yml` ve `README.md`'ye açık uyarı eklendi.

---

### 🏁 MVP TAMAMLANDI — 30 Temmuz 2026, 17:02

**Proje canlıda ve uçtan uca çalışıyor.** [streak-tracker.me](https://streak-tracker.me) · [api.streak-tracker.me](https://api.streak-tracker.me/health)

#### Canlı doğrulanan testler

| # | Test | Sonuç |
|---|---|---|
| 1 | Giriş → onboarding → dashboard (production OAuth) | ✅ |
| 2 | Test bildirimi → **telefona push** (bot kimliğiyle) | ✅ |
| 3 | Rozet → profil README'sinde render | ✅ |
| 6 | **Kalıcılık:** container'lar silinip yeniden oluşturuldu | ✅ DataProtection anahtarı korundu, token çözümleme hatası yok, oturum bozulmadı |

Dashboard'da streak kartları, katkı heatmap'i, rozet önizlemesi ve bildirim ayarları gerçek verilerle çalışıyor.

#### ⏳ Henüz denenmemiş testler

* **Zamanlanmış job'ın kendiliğinden çalışması.** Bugüne kadar bildirimler **hep elle** tetiklendi. `StreakCheckJob`'ın 20:00 UTC'de insan müdahalesi olmadan çalıştığı görülmedi. Test için: bugün commit atmamış olmak + bildirim saatini yakın bir saate almak gerekiyor.
* **Çok kullanıcılı akış.** Sistem yalnızca tek kullanıcıyla (`Berkowhiskey`) çalıştı. İkinci bir hesabın kaydı, kendi gizli reposu, kendi App kurulumu ve ayrı streak hesabı hiç denenmedi. Kullanıcı bu test için anonim bir hesap hazırladı.

#### Toplam maliyet: **aylık $0**

Oracle Cloud Always Free (süresiz) + Supabase (ücretsiz katman) + Vercel (ücretsiz) + Namecheap `.me` (Student Pack, 1 yıl ücretsiz — sonrasında ~$20/yıl veya DuckDNS'e geçiş).

#### Kapsam dışı bırakılanlar (sıradaki iş listesi)

Zaman dilimi desteği · Telegram/e-posta fallback · Milestone bildirimleri · Streak dondurma · Haftalık özet · Leaderboard · Public profil sayfası · Rozet çeşitleri · `NotificationService` / `GitHubAppService` birim testleri

---

#### 🔹 FAZ 9 — MVP Sonrası: Saat Dilimi · Milestone · İngilizce Dil Desteği

> **Tespit edilen dört eksik:** bot avatarının yanlış görünmesi, her şeyin UTC olması, ürünün tamamen Türkçe olması ve hiç kutlama bildirimi bulunmaması. Kullanıcının ek testleri de bu turda tamamlandı.

* **31 Temmuz 2026, 09:15** - ✅ **ÇOK KULLANICILI AKIŞ DOĞRULANDI (Test 5).** Kullanıcı, bir arkadaşının hesabı ve kendi yedek hesabıyla kayıt oldu; her iki hesapta da test bildirimleri ve heatmap doğru çalıştı. Sistem artık tek kullanıcı varsayımından çıktı.
* **31 Temmuz 2026, 09:30** - 🐞 **Bot avatarı tespiti.** Bildirimleri atan `streaktracker-notify[bot]`, kullanıcının kendi profil fotoğrafıyla görünüyordu. **Kök neden:** GitHub App'e logo yüklenmediğinde App **sahibinin avatarı** kullanılıyor. Kod hatası değil, yapılandırma eksiği — App ayarlarından logo yüklenerek çözülüyor (Aşama A, kullanıcıda).
* **31 Temmuz 2026, 10:20** - **[Aşama B] Saat dilimi desteği.**
  * `User.TimeZoneId` (IANA, varsayılan `UTC`) eklendi; `PreferredNotificationHourUtc` → **`PreferredNotificationHour`** olarak yeniden adlandırıldı ve artık *kullanıcının yerel saati* anlamına geliyor.
  * **Geriye dönük uyumluluk:** Migration yalnızca RENAME yapıyor ve `TimeZoneId` varsayılanı `UTC` olduğu için mevcut kayıtların davranışı **birebir korunuyor**. Doğrulandı: mevcut kullanıcının saati 20 olarak, TZ `UTC` olarak kaldı.
  * **Neden UTC'de saklamıyoruz:** Kullanıcı "20:00'da uyar" dediğinde yaz/kış saati değişse bile 20:00'da uyarılmalı. UTC'de saklamak DST geçişlerinde bir saatlik kaymaya yol açardı.
  * `Services/UserClock.cs` yazıldı — saf ve test edilebilir: `TodayIn`, `CurrentHourIn`, `HoursLeftInDay`, `StartOfTodayUtc`. Tanınmayan saat dilimi UTC'ye düşer (bildirimleri tamamen durdurmaktansa güvenli varsayılan).
  * `StreakService` ("bugün"), `NotificationService` (kalan saat, mükerrer kontrolü) ve saatlik job güncellendi. **Job artık SQL'de saat filtresi yapamıyor** — DST nedeniyle UTC ofseti sabit olmadığı için aday kullanıcılar çekilip eşleştirme bellekte yapılıyor.
  * ⚠️ **Dockerfile'a `tzdata` eklendi.** `TimeZoneInfo.FindSystemTimeZoneById` Linux'ta bu paket olmadan çalışmaz ve eksikliği ancak çalışma anında fark edilirdi.
  * **9 yeni test:** gece yarısı senaryosu (Türkiye'de 01:00 iken UTC'de hâlâ dün), batı yarıküre, DST geçişi (New York kış/yaz), UTC kullanıcılarının davranışının değişmediği.
* **31 Temmuz 2026, 11:05** - **[Aşama C] Milestone bildirimleri (7 / 30 / 100 / 365).**
  * `NotificationLog.MilestoneDay` (`int?`) eklendi; kutlama bildirimleri uyarılardan ayrışıyor.
  * **Akış değişikliği:** streak tazelendikten sonra önce milestone kontrol edilir; ulaşıldıysa uyarı yerine **kutlama** gönderilir.
  * **İnce nokta:** Mükerrer kontrolü "hiç kutlandı mı" değil, **mevcut serinin başlangıcından sonra kutlandı mı** diye bakıyor. Böylece seri kırılıp yeniden 7 güne ulaşılırsa bu yeni başarı tekrar kutlanıyor.
  * Kutlama mesajı bir şey yapmasını istemez, yalnızca tebrik eder; rekor kırıldıysa ayrıca belirtir.
* **31 Temmuz 2026, 12:30** - **[Aşama D] İngilizce dil desteği — arayüz + bildirimler + rozet.**
  * **Backend:** `Enums/AppLanguage.cs`, `User.Language` (DB'de metin olarak), `NotificationMessageBuilder`'ın üç metodu da iki dilli, `SvgBadgeService` etiket/tarih biçimi dile göre.
  * **Rozet:** `?lang=en` parametresi; verilmezse kullanıcının kayıtlı tercihi kullanılır. Böylece README'ye İngilizce rozet konabilirken kullanıcının kendi dili korunur.
  * ⚠️ **`ComputeETag`'e dil dahil edildi** — aksi halde kullanıcı dilini değiştirdiğinde tarayıcı önbellekteki eski dildeki rozeti göstermeye devam ederdi. Testle sabitlendi.
  * **Frontend:** `lib/i18n.ts` (tr/en sözlükleri), `LanguageProvider` + `LanguageSwitcher`. Tercih **çerezde** saklanıyor; böylece sunucu bileşenleri de okuyabiliyor ve sayfa doğru dille render ediliyor (dil yanıp sönmesi yok). Tüm sayfalar ve bileşenler çevrildi.
  * **Tip güvenliği:** Türkçe sözlük şema görevi görüyor; İngilizce sözlükte eksik veya fazla anahtar **derleme hatası** veriyor. (`as const` bilinçli olarak kullanılmadı — literal tipler İngilizce metinleri reddederdi.)
* **31 Temmuz 2026, 12:48** - **FAZ 9 KOD TARAFI TAMAMLANDI ✅**
  * `dotnet build` → **0 warning / 0 error**, `dotnet test` → **58/58 passed** (29 → 58, 29 yeni test)
  * Frontend `tsc --noEmit` → hatasız, `npm run build` → başarılı
  * Rozet doğrulaması: varsayılan Türkçe, `?lang=en` İngilizce, **ETag'ler farklı** (`a2a133e4…` / `5761f782…`), bulunamadı rozeti de iki dilli
  * 3 migration üretildi ve yerel veritabanına uygulandı: `AddUserTimeZone`, `AddMilestoneToNotificationLog`, `AddUserLanguage`
* **31 Temmuz 2026, 13:05** - ✅ **KVKK SİLME HAKKI CANLI DOĞRULANDI (Test 6).** Kullanıcı, yedek GitHub hesabını panelden "Hesabımı sil" ile sildi; production veritabanı (Supabase) uzaktan sorgulanarak kontrol edildi.
  * **Yöntem:** Sunucuda `psql` kurulu olmadığı için tek seferlik `postgres:16-alpine` container'ı ile bağlanıldı; kimlik bilgileri `.env`'den okundu, hiçbir yere yazılmadı.
  * **Sonuç:** `users` = 2 (yalnızca `Berkowhiskey` + `yunopo42`), silinen hesap tabloda **yok**. `streaks` = 2, `notification_logs` = 4 — **sahipsiz (orphan) kayıt 0**.
  * **Cascade doğrulandı:** `FK_streaks_users_UserId` ve `FK_notification_logs_users_UserId` → `delete_rule = CASCADE`. Kullanıcı silinince streak ve bildirim logları da gidiyor; arkada veri artığı kalmıyor.
  * Tasarım gereği GitHub hesabındaki `.streak-tracker-notifications` reposu **silinmiyor** — ona yalnızca kullanıcı karar verebilir, API yanıtında bu açıkça bildiriliyor.
  * ℹ️ Sunucuda ileride DB kontrolü için `postgres:16-alpine` imajı bırakıldı (~80 MB; disk 38 GB boş).
* **31 Temmuz 2026, 13:20** - 🎉 **SON İKİ AÇIK TEST DE KAPANDI — SİSTEM UÇTAN UCA OTONOM ÇALIŞIYOR**
  * ✅ **Test 4 — Zamanlanmış job kendiliğinden çalıştı.** Kullanıcı o gün bilinçli olarak commit atmadı; `StreakCheckJob` **hiçbir insan müdahalesi olmadan** tetiklendi, streak'i GitHub'dan tazeledi, bugün commit olmadığını gördü ve uyarı bildirimini telefona düşürdü.
    * **Önemi:** Bugüne kadar tüm bildirimler elle (`/notifications/test` veya `/check-now`) tetiklenmişti. Projenin asıl vaadi olan *"sen unutsan bile ben hatırlatırım"* zinciri — Hangfire recurring job → streak tazeleme → karar mantığı → GitHub App bot yorumu → GitHub Mobile push — ilk kez baştan sona kendi kendine çalıştı.
  * ✅ **Test 5 — Çok kullanıcılı akış genişletildi.** İki arkadaş hesabında daha kayıt, onboarding ve bildirim akışı sorunsuz çalıştı. Sistem artık toplam **4 farklı GitHub hesabıyla** doğrulanmış durumda; tek kullanıcı varsayımı tamamen kalktı.
  * **Sonuç:** MVP'nin "henüz denenmemiş" listesi **boşaldı**. Geriye yalnızca Faz 9'un canlıya alınması kaldı.
* **31 Temmuz 2026, 13:45** - 🐞 **Çözülen Hata (asistan kaynaklı): `resolveInitialLocale` sunucudan çağrılamıyordu.**
  * **Belirti:** Lokal ortamda sayfa açılırken `Runtime Error — Attempted to call resolveInitialLocale() from the server but resolveInitialLocale is on the client.`
  * **Kök neden:** Fonksiyon saf (yan etkisiz) olmasına rağmen `"use client"` işaretli `components/language-provider.tsx` içinde tanımlanmıştı. Next.js o dosyadaki **tüm** export'ları istemci bundle'ının parçası sayar; sunucu bileşenleri yalnızca bileşen olarak render edebilir, fonksiyon olarak çağıramaz.
  * **Neden build'de yakalanmadı:** `tsc` ve `npm run build` bu sınırı doğrulamıyor; hata ancak sunucu bileşeni gerçekten render edilirken ortaya çıkıyor. **Ders:** i18n gibi sunucu/istemci sınırında duran kod için build yeşil olması yeterli kanıt değil, sayfayı fiilen istemek gerekiyor.
  * **Düzeltme:** Fonksiyon direktifi olmayan `lib/i18n.ts`'e taşındı (`isLocale` ve `DEFAULT_LOCALE` zaten orada). Etkilenen üç sunucu bileşeninin (`app/layout.tsx`, `app/page.tsx`, `app/gizlilik/page.tsx`) import'ları güncellendi.
* **31 Temmuz 2026, 13:55** - **Dil desteğinin eksik parçası tamamlandı: sayfa başlıkları (`<title>`).**
  * **Tespit:** Gövde metinleri İngilizceye çevriliyordu ama sekme başlığı ve link önizlemesi her iki dilde de Türkçe kalıyordu — `export const metadata` **sabit** bir nesne olduğu için çerezi okuyamıyor.
  * **Düzeltme:** `layout.tsx` ve `gizlilik/page.tsx`'te `metadata` → **`generateMetadata()`** (async) yapıldı; çerezden dil okunup sözlükten üretiliyor. Sözlüğe `meta` bölümü eklendi (`siteTitle`, `siteDescription`, `privacyTitle`).
  * **Doğrulama (çalışan dev sunucusuna istek atarak):** `<html lang>` çerezle `tr`↔`en` değişiyor · gizlilik başlığı TR `Izinler ve Gizlilik` / EN `Permissions and Privacy` · sekme başlıkları da dile göre üretiliyor · `npx tsc --noEmit` hatasız.
* **31 Temmuz 2026, 14:20** - 🔍 **"Rozet ve bildirim İngilizceye geçmiyor" şikâyeti incelendi — ikisi de kod hatası değildi, ama araştırma gerçek bir kusuru açığa çıkardı.**
  * **Bildirim (hata yok, sıralama):** `notification_logs` ve `users.UpdatedAt` karşılaştırıldı → test bildirimi **11:01:57**'de gönderilmiş, dil **11:03:04**'te İngilizce yapılmış. Yani bildirim atıldığında tercih hâlâ Türkçeydi; sistem doğru davranmış.
  * **Rozet (hata yok, önbellek):** Lokal API `curl` ile sorgulandığında **İngilizce** dönüyordu (`day streak`, `RECORD`); production ise Türkçe — çünkü Faz 9 henüz deploy edilmedi. Tarayıcıda Türkçe görünmesinin sebebi `Cache-Control: public, max-age=300`.
  * 🐞 **Ortaya çıkan gerçek kusur:** **Rozet URL'inde dil bilgisi yoktu.** `ComputeETag`'e dili eklemek bunu çözmüyor — ETag yalnızca tarayıcı *sorduğunda* devreye girer, `max-age` dolmadan istek hiç atılmaz. Daha ağırı: README'ye kopyalanan kodda da dil yoktu ve GitHub rozetleri kendi **camo proxy**'si üzerinden çok daha uzun süre önbelleklediği için, kullanıcı dilini değiştirse bile profilindeki rozet uzun süre eski dilde kalırdı.
  * **Düzeltme — dil artık URL'in parçası:**
    * `GET /users/me/badge` **`?lang=` parametresi** alıyor; verilmezse kullanıcının kayıtlı tercihi kullanılıyor. Üretilen Markdown/HTML kodları artık `?lang=tr|en` içeriyor.
    * **Neden parametre:** Arayüz hangi dili istediğini zaten biliyor. Yalnızca DB'deki tercihe bakılsaydı, dil değiştirildikten hemen sonra kod parçacığı istendiğinde `PATCH /preferences` henüz tamamlanmamış olabilir ve **eski dil dönerdi** (yarış durumu).
    * Dashboard'daki rozet önizlemesi `?lang=${locale}` ile isteniyor → adres değiştiği için tarayıcı önbelleği devreye girmiyor, dil değişimi **anında** yansıyor.
    * Dil değişince kod parçacıkları yeniden üretiliyor (yalnızca onlar; takvim GitHub'a gittiği için sayfanın tamamı yeniden yüklenmiyor).
  * **Doğrulama:** `dotnet build` → **0 warning / 0 error**, `dotnet test` → **58/58 passed**, frontend `tsc --noEmit` → hatasız.
  * ℹ️ **Not:** Çalışan `dotnet run` süreci `bin/`'i kilitlediği için build geçici bir çıktı dizinine alındı (`-p:BaseOutputPath`); kullanıcının ortamına dokunulmadı.

---

#### 🔹 FAZ 9 — YAYINA ALMA ✅

* **31 Temmuz 2026, 14:45** - ⚠️ **Deploy sırasında yakalanan risk: sunucuda commit edilmemiş yerel değişiklikler.** `git pull` *"Your local changes to `.env.example` and `docker-compose.prod.yml` would be overwritten"* diyerek durdu.
  * **Neden kritikti:** Sunucudaki `docker-compose.prod.yml`, 30 Temmuz'da **Supabase'e göre elle düzenlenmişti** ve sunucu o gün Faz 8 commit'ini hiç almamıştı (`242349a` = Faz 7'de kalmış). Körlemesine `git checkout` yapılsaydı dosya yerel `postgres` container'lı sürüme dönebilir, uygulama **boş bir veritabanına** bağlanıp migration'ları oraya uygulayabilirdi.
  * **İzlenen yol:** Önce `docker-compose.prod.yml`, `.env.example` ve `.env` `~/deploy-backup/` altına yedeklendi → sonra checkout + pull → ardından **yedekle yeni sürüm `diff` ile karşılaştırıldı.**
  * **Sonuç:** Tek fark **8 satır yorum** (secrets izin uyarısı); Supabase bağlantı yapılandırması repo sürümünde zaten mevcuttu (Faz 8'de commit edilmiş). İşlevsel kayıp yok.
  * **Ders:** Sunucuya elle yapılan düzenlemeler commit edilene kadar `git pull` her zaman veri kaybı riski taşır. Doğrulanmadan üzerine yazılmamalı.
* **31 Temmuz 2026, 14:50** - 🚀 **FAZ 9 CANLIYA ALINDI.** `docker compose -f docker-compose.prod.yml up -d --build` → imaj yeniden derlendi (Dockerfile'a `tzdata` eklendiği için gerekliydi), API container'ı yeniden oluşturuldu.
  * **3 migration Supabase'e uygulandı:** `AddUserTimeZone` · `AddMilestoneToNotificationLog` · `AddUserLanguage`
  * **Geriye dönük uyumluluk canlıda doğrulandı:** Üç kullanıcının da bildirim saati korundu (10 / 14 / 20), `TimeZoneId=UTC` ve `Language=Turkish` varsayılanlarıyla geldi — yani mevcut kullanıcıların davranışı **hiç değişmedi**.
* **31 Temmuz 2026, 14:56** - ✅ **CANLI DOĞRULAMA TAMAMLANDI**

| Kontrol | Sonuç |
|---|---|
| `GET /health` | **200** `{"status":"healthy","database":"connected"}` |
| Rozet (varsayılan) | **200** · Türkçe (`gunluk seri`, `REKOR`) · ETag `b16d889f8810c914` |
| Rozet `?lang=en` | **200** · İngilizce (`day streak`, `RECORD`) · ETag `6c8b4573b03bb1ff` — **farklı** ✓ |
| `If-None-Match` ile tekrar | **304 Not Modified** |
| `GET /users/me` (token'sız) | **401** |
| `/swagger` | **404** (production'da kapalı) |
| Frontend (Vercel) | **200** · `<html lang="tr">` · başlık *StreakTracker — GitHub Serini Kaybetme* |
| Frontend (`lang=en` çerezi) | `<html lang="en">` · başlık *Don't Break Your GitHub Streak* — **Vercel otomatik deploy'u da güncel** ✓ |

---

#### 🔹 FAZ 10 — 🐞 KRİTİK HATA: "Bugün commit atıldı" hiç görünmüyordu

> **Belirtisi:** Kullanıcı commit/push attı, GitHub profilinde katkı göründü, ama StreakTracker 15+ dakika boyunca "bugün commit yok" demeye devam etti. Heatmap'te "Yenile" de işe yaramadı, test bildirimi de "henüz commit atılmadı" dedi. **Bu, ürünün temel vaadini bozan bir hataydı.**

* **31 Temmuz 2026, 15:10** - **Eleme yöntemiyle teşhis.** Sırayla doğrulandı:
  * Commit push edilmiş mi → ✅ `PushEvent` 11:48:27Z
  * GitHub commit'i hesapla eşleştirmiş mi (e-posta eşleşmesi) → ✅ `author: Berkowhiskey`
  * GitHub katkı olarak saymış mı → ✅ profil takvimi: *"1 contribution on July 31st"*
  * Bizim GraphQL isteği hata mı alıyor → ❌ HTTP **200**, hata yok, ama bugün boş
  * **İlk hipotez (GitHub önbelleği) YANLIŞ çıktı** ve kullanıcının paylaştığı `calendar?days=3` çıktısı onu çürüttü: dar aralıkta bugün `contributionCount: 1` geliyordu. Yani sorun sorgunun kendisinde değil, **aralığın genişliğindeydi.**
* **31 Temmuz 2026, 15:15** - 🔬 **Kök neden kanıtlandı — GitHub'ın 1 yıl sınırı.** Aynı gün, aynı kullanıcı, yalnızca pencere genişliği farklı:

  ```
  calendar?days=364 -> {"date":"2026-07-31","contributionCount":0}
  calendar?days=363 -> {"date":"2026-07-31","contributionCount":1}
  ```

  * `today.AddDays(-364)` ile `today` arası **iki uç dahil 365 gün** eder. `contributionsCollection` en fazla 1 yıllık aralık kabul ediyor ve aşıldığında **hata vermiyor**: son günü takvimde tutuyor ama katkı sayısını **0** döndürüyor.
  * **Neden bu kadar sinsi:** Dün ve öncesi hep doğruydu, yalnızca *bugün* yanlıştı — yani streak geçmişi ve heatmap doğru görünüyordu. Hata ancak "bugün commit attım ama sistem görmüyor" denince fark edilebilirdi. Faz 2'den beri mevcut olması muhtemel; MVP boyunca bildirimler hep elle tetiklendiği ve `HasCommittedToday` genelde erken saatlerde hesaplandığı için gözden kaçmış olabilir.
* **31 Temmuz 2026, 15:25** - **Düzeltmeler:**
  1. `StreakService.ContributionWindowDays` **364 → 363** (pencere 364 güne iner, sınırın altında kalır). Kanıt niteliğindeki iki çıktı koda yorum olarak işlendi.
  2. `StreaksController.DefaultCalendarDays` **364 → 363** — heatmap de aynı hatayı yaşıyordu; kullanıcının "Yenile" denemelerinin neden sonuç vermediğinin açıklaması budur.
  3. `GitHubService.GetContributionDaysAsync` artık bitiş zamanını **şu ana kırpıyor** — asıl sebep bu değildi, ancak gün sonuna (`23:59:59Z`) sorgu atmak geleceğe sorgu atmak demekti.
  4. 🐞 **Yan hata yakalandı:** `StreaksController.Calendar` "bugün"ü `DateTime.UtcNow` ile hesaplıyordu; streak servisi ise `UserClock` kullanıyor. Saat dilimi seçen bir kullanıcıda **takvim ile streak birbirini tutmayacaktı**. `UserClock`'a geçirildi.
  5. 🐞 **Kırılma önlendi:** `frontend/lib/api.ts` içindeki `getCalendar(days = 364)` varsayılanı, backend'in yeni üst sınırı (363) nedeniyle **400 döndürüp dashboard heatmap'ini kıracaktı**. Varsayılan tamamen kaldırıldı — gün sayısı artık tek bir yerde (backend'de) tanımlı.
  * **Doğrulama:** `dotnet build` → **0 warning / 0 error**, `dotnet test` → **58/58 passed**, frontend `tsc --noEmit` → hatasız.
* **31 Temmuz 2026, 15:45** - ✅ **FAZ 10 CANLIYA ALINDI VE DOĞRULANDI.** Sunucuda `git pull` (çalışma alanı temiz) + rebuild; API sorunsuz ayağa kalktı, `/health` → **200**.
  * Kullanıcı panelden "Yenile" dedi ve **hata düzeldi:** `CurrentStreak 3 → 4`, `HasCommittedToday: f → t`, `LastCommitDate: 2026-07-30 → 2026-07-31`.
  * Rozet zinciri uçtan uca doğrulandı: veritabanı **4** · rozet API **4** · **GitHub camo proxy 4** (`Age: 234`, bizim `max-age=300` başlığımıza uyuyor).
  * ℹ️ **Kullanıcının rozeti eski görmesinin sebebi kendi tarayıcı önbelleğiydi** (`Ctrl+Shift+R` çözüyor). Ölçüm sırasında öğrenilen faydalı bilgi: camo upstream `Cache-Control` başlığına saygı gösteriyor — aynı profildeki shields.io rozeti `max-age=86400` gönderdiği için 24 saat takılı kalırken, bizim rozet en fazla **5 dakika** gecikiyor.


---

#### 🔹 FAZ 11 — Test Kapsamının Genişletilmesi (bağımlılığı olan servisler)

> **Gerekçe:** Faz 10'daki hata, 58 testin hepsinin **saf sınıflar** (`StreakCalculator`, `UserClock`, `NotificationMessageBuilder`, `SvgBadgeService`) için yazıldığını, bağımlılığı olan servislerin (`StreakService`, `NotificationService`, `GitHubService`) hiç test edilmediğini açığa çıkardı. Hata da tam olarak o katmanda çıkmıştı. Kullanıcı sıradaki iş olarak yeni özellik yerine **test kapsamını** seçti.

* **1 Ağustos 2026, 11:20** - **Test altyapısı kuruldu.** `NSubstitute 5.3.0` (sahte bağımlılık) ve `Microsoft.EntityFrameworkCore.InMemory 9.0.9` eklendi. `TestSupport.cs` yazıldı: her teste **izole** InMemory veritabanı (`Guid` adlı) ve varsayılan kullanıcı üreteci.
  * `PassThroughTokenProtector` — DataProtection altyapısını ayağa kaldırmadan `AppDbContext` oluşturulabilmesi için.
* **1 Ağustos 2026, 11:30** - **`StreakServiceTests` (7 test).** En kritiği **Faz 10 hatasının regresyon testi**: katkı penceresi iki uç dahil **364 günü aşamaz**.
  * Ayrıca: pencerenin gereksiz daraltılmadığı, bitişin kullanıcının *bugünü* olduğu (saat dilimiyle), streak kaydının yoksa oluşturulduğu, rekorun asla düşürülmediği.
* **1 Ağustos 2026, 11:40** - **`NotificationServiceTests` (14 test).** Ürünün "bildirim gönderilsin mi" kararı ilk kez test altına alındı: bugün commit varsa gönderilmemesi, aynı gün mükerrer gönderilmemesi, **test bildiriminin o günün gerçek uyarısını engellememesi**, milestone kutlamasının uyarıdan önce gelmesi, seri kırılıp yeniden ulaşılınca tekrar kutlanması, App kurulu değilse "gönderildi" denmemesi, başarısız denemelerin de loglanması ve **bir kullanıcıdaki hatanın saatlik turu durdurmaması**.
* **1 Ağustos 2026, 11:45** - **`GitHubServiceTests` (7 test).** GraphQL isteğinin nasıl kurulduğu ilk kez doğrulanıyor: bitişin geleceğe taşmaması, geçmiş gün sorgusunda günün tamamının kapsanması, başlangıcın gün başı olması, yanıtın çözümlenmesi, **rate-limit'in ayırt edilmesi** ve HTTP hatalarının `GitHubServiceException` olarak yüzeye çıkması.
  * Gerçek HTTP çağrısı yapılmıyor; `HttpMessageHandler` türetilerek giden istek yakalanıyor.
  * 🐞 **Test hatası yakalandı (kod değil):** `DateTime.Parse` `Z` ekini görüp değeri **makinenin yerel saatine** çeviriyor; testler TR'de (+3) kayıyordu. `AdjustToUniversal | AssumeUniversal` ile açıkça UTC istendi. Aksi halde test, çalıştığı makinenin saat dilimine göre sonuç verirdi.
* **1 Ağustos 2026, 11:50** - ✅ **TESTLERİN GERÇEKTEN KORUDUĞU KANITLANDI.** Yeşil test tek başına kanıt değildir; üç kritik koruma bilinçli olarak bozulup **kırmızıya döndüğü görüldü**, sonra geri alındı:

| Bozulan davranış | Beklenen | Sonuç |
|---|---|---|
| `ContributionWindowDays` 363 → 364 | Pencere testi kırmızı | ✅ *"Katki penceresi 365 gun…"* |
| `HasBeenNotifiedTodayAsync`'ten `!n.IsTest` kaldırıldı | Test-bildirimi testi kırmızı | ✅ yalnızca o test |
| `GitHubService`'te `to` kırpması kaldırıldı | Gelecek-zaman testi kırmızı | ✅ yalnızca o test |

  * ⚠️ **İlk yazdığım regresyon testi hatayı YAKALAMIYORDU:** eşiği `<= 365` koymuştum, oysa ampirik olarak 365'in kendisi bozuk davranıyor. Bozma denemesi olmasaydı bu, "koruyormuş gibi görünen" ölü bir test olarak kalacaktı. Eşik `<= 364` yapıldı.
* **1 Ağustos 2026, 11:52** - **FAZ 11 TAMAMLANDI ✅** `dotnet build` → **0 warning / 0 error**, `dotnet test` → **86/86 passed** (58 → 86, **28 yeni test**).

| Test dosyası | Test |
|---|---|
| `SvgBadgeServiceTests` | 15 |
| `NotificationServiceTests` | **14 (yeni)** |
| `NotificationMessageBuilderTests` | 11 |
| `StreakCalculatorTests` | 10 |
| `UserClockTests` | 9 |
| `StreakServiceTests` | **7 (yeni)** |
| `GitHubServiceTests` | **7 (yeni)** |

---

#### 🔹 FAZ 12 — Rozet Paketi: Animasyon · Temalar · Rütbe · Kompakt Boyut 🎨

> **Neden rozetle başlandı:** Dashboard'ı yalnızca kullanıcının kendisi görüyor; rozet ise başkalarının profil README'sinde duruyor. Projenin tek organik görünürlük alanı orası.

* **1 Ağustos 2026, 16:50** - **`BadgeRenderOptions` kaydı eklendi.** Tema + dil + varyant + animasyon tek bir kayıtta toplandı.
  * **Gerekçe:** `ISvgBadgeService`'in üç metodu da ayrı ayrı parametre alıyordu; her yeni görünüm seçeneğinde tüm imzalar değişecekti. Artık seçenek eklemek imza değiştirmiyor.
* **1 Ağustos 2026, 16:55** - **[Tema] Dört yeni tema:** `dracula`, `tokyo-night`, `nord`, `catppuccin` (mevcut `dark`/`light` yanına). Renkler ilgili paletlerin resmi değerlerinden alındı. `BadgePalette` zaten ayrı bir yapı olduğu için maliyet yalnızca renk tanımı oldu.
* **1 Ağustos 2026, 17:00** - **[Animasyon] Alev artık "nefes alıyor."**
  * **SMIL değil CSS tercih edildi.** İkisi de `<img>` bağlamında çalışıyor (JavaScript çalışmıyor), ancak CSS **`prefers-reduced-motion`** desteği veriyor: işletim sisteminde "hareketi azalt" seçili kullanıcılarda animasyon kendiliğinden duruyor. Erişilebilirlik açısından belirleyici fark bu oldu.
  * **Yalnızca `opacity` oynatılıyor.** `transform` tabanlı bir titreme, alev grup içinde ölçeklendiği için dönüşüm merkezini kaydırır ve alev yerinden oynardı.
  * Dış alev ve iç çekirdek **farklı sürelerle** (2.8s / 1.9s) titriyor — eş zamanlı olsalardı mekanik görünürdü.
  * Serisi olmayan kullanıcıda animasyon hiç üretilmiyor; sönük bir alevin titremesi anlamsız olurdu.
* **1 Ağustos 2026, 17:05** - **[Rütbe] Seriye göre unvan:** Kıvılcım → Alev → Ateş → Yangın → Efsane (EN: Spark → Flame → Fire → Blaze → Legend).
  * **Eşikler bilinçli olarak milestone bildirimleriyle aynı: 1 / 7 / 30 / 100 / 365.** Kullanıcı kutlama bildirimini aldığında rozetinde de karşılığını görüyor; iki sistem birbirini doğruluyor.
  * Serisi olmayan kullanıcıda rütbe **hiç çizilmiyor** — "rütbesizsin" demektense sessiz kalmak tercih edildi.
* **1 Ağustos 2026, 17:10** - **[Kompakt] `?variant=compact`** — 190×52 boyutunda, yalnızca alev + seri. README'de yan yana birden çok rozet dizmek isteyenler için.
* **1 Ağustos 2026, 17:15** - **Panelden seçilebilir hale getirildi.** Dashboard'a tema ve boyut seçicisi eklendi; seçim anında önizlemeye ve kopyalanacak Markdown/HTML koduna yansıyor.
  * ⚠️ **Görünümü belirleyen her şey URL'e yazılıyor** (`?theme=`, `?variant=`, `?lang=`). Faz 9'da dil için öğrenilen ders burada baştan uygulandı: rozet uzun süre önbelleklendiği (tarayıcı `max-age`, GitHub camo proxy) için aynı adresin farklı içerik döndürmesi, kullanıcının değişikliği günlerce görememesi demek.
  * `ComputeETag` imzasına **varyant ve animasyon** da eklendi; testle sabitlendi.
* **1 Ağustos 2026, 17:25** - **FAZ 12 TAMAMLANDI ✅**
  * `dotnet build` → **0 warning / 0 error**, `dotnet test` → **131/131 passed** (86 → 131, **45 yeni test**)
  * Frontend `tsc --noEmit` → hatasız, `npm run build` → başarılı
  * Üretilen rozetler gözle doğrulandı; örnekler `rozet-onizleme/` klasörüne kaydedildi (6 tema + kompakt + İngilizce).
  * 🐞 **Çözülen hata (ortam):** Kesinti sırasında yarım kalan `.next` önbelleği yüzünden `npm run build` *"module not found: jetbrains_mono"* veriyordu. `.next` silinip yeniden derlendi; kod kaynaklı değildi.

**Yeni rozet adresi örnekleri:**

```
/api/v1/badges/{kullanici}.svg
/api/v1/badges/{kullanici}.svg?theme=dracula
/api/v1/badges/{kullanici}.svg?theme=tokyo-night&variant=compact
/api/v1/badges/{kullanici}.svg?lang=en&animated=false
```

* **1 Ağustos 2026, 19:05** - 🚀 **FAZ 11 + 12 CANLIYA ALINDI VE DOĞRULANDI.** Sunucuda `git pull` (çalışma alanı temiz) + rebuild; migration yok, yalnızca kod. API sorunsuz ayağa kalktı.

| Kontrol | Sonuç |
|---|---|
| `GET /health` | **200** `{"status":"healthy","database":"connected"}` |
| Rozet — varsayılan (dark) | `#0d1117` · animasyon (`@keyframes st-flicker`) · `prefers-reduced-motion` ✓ |
| Rozet — `?theme=dracula` | `#282a36` · ETag **farklı** (`a7d26e5b…` ≠ `535d30bb…`) |
| Rozet — `?theme=tokyo-night&variant=compact` | **190×52** boyutunda döndü |
| Rozet — `?animated=false` | `@keyframes` sayısı **0** (animasyon kapandı) |
| Rütbe — kayıtlı tercih (EN) | **SPARK** |
| Rütbe — `?lang=tr` | **KIVILCIM** (aynı seri, dile göre çeviriliyor) |
| `If-None-Match` | **304 Not Modified** |
| Geçerli XML — 6 temanın hepsi | dark · light · dracula · tokyo-night · nord · catppuccin → **hepsi OK** |
| Kayıtsız kullanıcı rozeti | bilgilendirici rozet döndü (kırık resim yok) |
| Frontend (Vercel) | **200** — otomatik deploy güncel |
| `/users/me` token'sız · `/swagger` | **401** · **404** |

* **1 Ağustos 2026, 19:06** - **Sıradaki tur planlandı: rozet özelleştirme.** Kullanıcı, panelde "Rozeti Özelleştir" sayfası istedi. Teknik değerlendirme yapıldı ve kapsam bilinçli olarak daraltıldı:
  * ✅ **Öncelikli:** alev rengi ve şekilleri · alev ekipmanları (meşale vb.) · **max boyut** (README'de tam genişlik) · arka plan/tema renkleri
  * ⏸️ **Ertelendi (kullanıcı kararı):** yazı tipi ve **glassmorphism / liquid glass**
    * **Font kısıtı:** GitHub rozeti `<img>` olarak işlediği için SVG **harici kaynak yükleyemez** — Google Fonts sessizce yok sayılır. Seçenekler: sistem fontları (0 KB, ~3 seçenek) · base64 gömme (rozet 2 KB → 30-60 KB) · metni path'e çevirme (sunucuda font render gerekir).
    * **Glassmorphism gerçek anlamda imkansız:** özü `backdrop-filter` ile arkasındaki içeriği bulanıklaştırmaktır; `<img>` olarak gömülen rozetin arkasında erişebileceği bir içerik yoktur. Yarı saydam panel + parlama + kenar ışığı ile ikna edici bir *taklidi* yapılabilir, ama "gerçek buzlu cam" olmaz.
  * ⚠️ **Baştan alınan mimari karar — imzalı kısa URL.** Seçenekler arttıkça adres (`?theme=&flame=&font=&size=&c1=&c2=`) kullanılamaz hale gelir. Ayarlar veritabanında tutulup adrese kısa bir **imza** (`?s=a4f2`) konacak: ayar değişince imza değişir, önbellek kendiliğinden tazelenir. Yalnızca DB'ye bakılsaydı Faz 9/10'daki tuzağa düşerdik — aynı adres farklı içerik döndürdüğü için kullanıcı değişikliği günlerce göremezdi.
  * **Planlanan fazlar:** 13 — ayar altyapısı + imzalı URL + max boyut + özelleştirme sayfası ve canlı önizleme · 14 — alev kütüphanesi (şekil, renk, ekipman) · 15 — modern stiller (neomorphism, claymorphism, cam görünümü).

---

#### 🔹 FAZ 13 — Rozet Özelleştirme: Ayar Altyapısı · Max Boyut · Özelleştirme Sayfası 🎨

> **Kapsam kararı:** Kullanıcı, yazı tipi ve glassmorphism'i bilinçli olarak erteledi (ikisi de en pahalı, en az getirili). Bu tur alev rengi, boyut ve arka plan renklerine odaklandı.

* **1 Ağustos 2026, 20:40** - **[Altyapı] `BadgeSettings` + imzalı URL.** Görünüm tercihleri `User.BadgeSettingsJson` alanında JSON olarak saklanıyor; adrese yalnızca 8 karakterlik bir imza (`?s=a4f2`) yazılıyor.
  * **Neden JSON kolon:** Görünüm seçenekleri sık değişiyor (tema, renk, boyut, ileride alev şekli ve ekipmanlar). Her seçenek için kolon açmak her turda migration demekti. Bu veri yalnızca okunup çiziliyor, üzerinde sorgu yapılmıyor.
  * ⚠️ **İmzanın görevi doğrulama değil, önbellek tazelemek.** Ayarlar yalnızca veritabanında tutulup adres sabit kalsaydı, Faz 9/10'daki tuzağa düşerdik: aynı adres farklı içerik döndürdüğü için kullanıcı görünümünü değiştirdiğinde profilindeki rozet uzun süre eski kalırdı. Ayar değişince imza değişiyor ve tarayıcı/camo adresi yeni kaynak sayıyor.
  * `AddBadgeSettings` migration'ı: **iki nullable kolon**, mevcut kullanıcılar etkilenmiyor (null → varsayılan görünüm).
* **1 Ağustos 2026, 20:55** - 🔐 **[Güvenlik] Renk doğrulama — beyaz liste.** Kullanıcının seçtiği renk **doğrudan SVG metnine** yazılıyor; serbest metne izin verilseydi tırnak kapatılıp öznitelik veya eleman enjekte edilebilirdi.
  * Yalnızca `#rgb` ve `#rrggbb` kabul ediliyor. **Kaçışlamak yerine reddetmek** tercih edildi: desene uymayan değer sessizce temanın rengine düşüyor.
  * **İki katmanlı savunma:** hem kaydederken (`Sanitized()`) hem çizerken (`ResolvePalette()`) doğrulanıyor. Kayıt katmanı bir gün atlansa bile SVG'ye geçersiz değer sızmıyor.
* **1 Ağustos 2026, 21:10** - **[Max boyut] `?variant=max`** — 850×200, README'de bir başlık alanını kaplıyor. Büyük tipografi, gradyanlı seri sayısı, geniş rütbe etiketi.
  * ℹ️ **Mini heatmap bilinçli olarak yapılmadı:** katkı takvimi veritabanında saklanmıyor, çizmek için her rozet isteğinde GitHub'a gitmek gerekirdi. Bu, projenin "GitHub'a hiç gitmeden milisaniyede render" ilkesini bozardı.
* **1 Ağustos 2026, 21:30** - **[Arayüz] `/dashboard/rozet` — Rozeti Özelleştir sayfası.** Canlı önizleme + boyut, tema, dört renk (alev üstü/altı, arka plan, kenarlık) ve animasyon anahtarı. Panelde artık tek bir "🎨 Rozeti özelleştir" düğmesi var; tema/boyut seçicileri buraya taşındı.
  * **Renkler önizlemede de adres parametresi olarak gönderiliyor** (`?flameFrom=&bg=…`). Yalnızca veritabanına bakılsaydı kullanıcı **kaydetmeden önizleme göremezdi**. Panelde uzun adres sorun değil; kısa imzalı adres yalnızca README'ye kopyalanan kod için kullanılıyor.
  * Kaydettikten sonra README kodu yeniden üretiliyor — yeni imzayı içermesi için.
* **1 Ağustos 2026, 22:00** - ✅ **TESTLERİN KORUDUĞU KANITLANDI.** Renk doğrulaması bilinçli olarak devre dışı bırakıldı; enjeksiyon testleri **kırmızıya döndü** (hem ayar katmanı hem çizim katmanı), sonra geri alındı. Güvenlik testi, açığı gerçekten yakaladığı gösterilmeden değersizdir.
* **1 Ağustos 2026, 22:03** - **FAZ 13 TAMAMLANDI ✅**
  * `dotnet build` → **0 warning / 0 error**, `dotnet test` → **169/169 passed** (131 → 169, **38 yeni test**)
  * Frontend `tsc --noEmit` → hatasız, `npm run build` → başarılı (yeni sayfa: `/dashboard/rozet`)
  * Üretilen rozetler gözle doğrulandı: `rozet-max.svg`, `rozet-max-dracula.svg`, `rozet-ozel-renk.svg`, `rozet-max-ozel-renk.svg` → hepsi geçerli XML, özel renkler temanın renklerini doğru eziyor.
* **1 Ağustos 2026, 23:05** - 🐞 **Çözülen Hata (asistan kaynaklı): özelleştirme sayfası açılırken çöküyordu.**
  * **Belirti:** `/dashboard/rozet` → `Runtime TypeError: Cannot read properties of undefined (reading 'width')`
  * **Kök neden:** `BadgeSettings` kaydı arayüze **doğrudan** döndürülüyordu ve içindeki `enum`'lar JSON'a **sayı** olarak yazılıyordu (`{"theme":0,"variant":0}`). Arayüz ise `"dark"` / `"full"` bekliyordu; `PREVIEW_SIZE[0]` → `undefined` → çökme.
  * **Neden `tsc` yakalamadı:** TypeScript tarafında dönen değer `BadgeSettings` olarak **beyan edilmişti**; derleyici çalışma zamanındaki gerçek JSON'u göremez. Sunucu-istemci sınırında tip beyanı tek başına güvence değil.
  * **Düzeltme:** API artık `BadgeSettingsDto` döndürüyor; enum'lar `ToCode()` ile metne çevriliyor (`dark`, `tokyo-night`, `max`).
    * **Ek fayda:** Sayı döndürmek, enum sıralaması ileride değişirse kayıtlı tercihlerin **sessizce başka bir temaya kayması** demekti. Metin bu riski de kapatıyor.
  * `UsersController.ThemeQueryValue` içindeki tekrar eden dönüşüm de yeni `ToCode()`'a bağlandı.
  * Arayüze savunma eklendi: `PREVIEW_SIZE[variant] ?? PREVIEW_SIZE.full` — beklenmedik bir değer sayfayı bir daha düşürmeyecek.
  * **11 yeni test** bu hatayı sabitliyor: her tema/varyant için metin karşılığı ve **metnin geri aynı enum'a çözülmesi** (aksi halde kaydedilen tercih bir sonraki açılışta kaybolurdu).
  * **Doğrulama:** `dotnet test` → **180/180 passed** (169 → 180), frontend `tsc --noEmit` → hatasız.

---

#### 🔹 FAZ 13 — YAYINA ALMA ✅

* **3 Ağustos 2026, 13:50** - 🔌 **Deploy öncesi engel: SSH bağlantısı kurulamadı.** `ssh` → `Connection timed out`.
  * **Teşhis (eleme yöntemiyle):** Sunucunun **443 portu açık** ve site çalışıyordu (`/health` → 200), yalnızca **22 kapalıydı**. Ardından `github.com:22` denendi — o da kapalı çıktı.
  * **Sonuç: Sorun sunucuda değil, kullanıcının o anki ağındaydı** (ev/ISP bağlantısı 22 portunu engelliyor). Önceki deploy'lar mobil hotspot üzerinden yapılmıştı.
  * **Çözüm:** Kullanıcı mobil hotspot'a geçti, bağlantı kuruldu. *(Kalıcı seçenek olarak SSH'a ikinci bir port (2222) açmak not edildi.)*
  * **Ders:** "Sunucuya erişemiyorum" her zaman sunucu sorunu değildir. Başka bir hedefin aynı portunu denemek, sorunu tek adımda ağ/sunucu diye ikiye ayırıyor.
* **3 Ağustos 2026, 13:58** - 🚀 **FAZ 13 CANLIYA ALINDI VE DOĞRULANDI.**
  * `AddBadgeSettings` migration'ı Supabase'e uygulandı. **Üç kullanıcının da ayarı `null`** (varsayılan görünüm) — geriye dönük uyumluluk canlıda doğrulandı.

| Kontrol | Sonuç |
|---|---|
| `GET /health` | **200** `{"status":"healthy","database":"connected"}` |
| Rozet — `?variant=max` | `width="850" height="200"` ✓ |
| Rozet — özel renk (`?flameFrom=&flameTo=&bg=`) | Üç özel renk de uygulandı; temanın `#0d1117` arka planı **ezildi** ✓ |
| 🔐 **Enjeksiyon denemesi** (`?bg=%23000"/><script>…`) | Üretilen SVG'de `<script>` sayısı **0** ✓ |
| `GET /users/me/badge-settings` (token'sız) | **401** |
| Max rozet — geçerli XML (dracula) | **OK** |
| `/dashboard/rozet` (Vercel) | **200** — otomatik deploy güncel |
| `/swagger` | **404** (production'da kapalı) |

* **3 Ağustos 2026, 14:30** - **[Faz 14 hazırlığı] Rozet görsel kütüphanesi klasörleri kuruldu.**
  * `Assets/Badges/flames/` (alev şekilleri) ve `Assets/Badges/accessories/` (meşale, taç vb.) oluşturuldu; her birine format örneği bir dosya kondu (`classic.svg`, `torch.svg`) ve kullanıcı için `README.md` rehberi yazıldı.
  * **Gömülü kaynak (`EmbeddedResource`) tercih edildi**, dosya kopyalama değil: Docker imajında yol sorunu çıkmaz ve rozet üretiminde disk okuması yapılmaz — "milisaniyede render" ilkesi korunur.
  * ✅ **4 otomatik doğrulama testi eklendi.** Kullanıcı klasöre dosya attığında `dotnet test` bunları hemen denetliyor:
    * dosyalar pakete gerçekten gömülüyor mu,
    * hepsi geçerli XML mi (bozuk SVG rozeti komple kırar),
    * **sabit renk içeriyor mu** (`fill="#..."` varsa kullanıcının renk seçimi çalışmaz — Game Icons dosyalarında bu öntanımlı gelir),
    * her dosyada en az bir `<path>` var mı (boş dosya rozeti sessizce bozar).
  * `dotnet test` → **184/184 passed** (180 → 184).

---

#### 🔹 FAZ 14 — Rütbe Alevleri: Şekil Seçilmez, Kazanılır 🔥

> **Kullanıcının tasarım değişikliği:** Alev şekli başta *seçilebilir* bir özellik olarak planlanmıştı. Kullanıcı bunun yerine **rütbeye bağlanmasını** önerdi — ve bu daha iyi bir tasarım çıktı.
>
> **Gerekçe:** Seçilebilir olsaydı herkes en görkemli alevi seçerdi ve rütbe sistemi anlamını yitirirdi; 6 günlük kullanıcı ile 365 günlük kullanıcı aynı görünürdü. Kazanılan görünüm, seçilen görünümden daha değerlidir — ayrıca rozet, seriyi tek bakışta anlatan bir **sosyal işarete** dönüşür. Özgürlük kaybı yok: şekil kazanılıyor, **renkler hâlâ seçilebiliyor**.

* **3 Ağustos 2026, 15:10** - **Kullanıcı 4 alev ekledi** (`candle-light`, `burning-embers`, `celebration-fire`, `volcano`) — hepsi Game Icons'tan.
  * ✅ **Faz 13'te eklenen doğrulama testi işini yaptı:** dosyalar `fill="#fff"` ve siyah arka plan katmanı (`M0 0h512v512H0z`) ile geldiği için test **kırmızı döndü ve dosya adını söyledi**. Tahmin etmeye gerek kalmadı.
  * Dosyalar temizlendi: arka plan katmanı, sabit renkler ve gereksiz sarmalayıcılar kaldırıldı.
* **3 Ağustos 2026, 15:20** - **`FlameLibrary` yazıldı.** Gömülü SVG'ler açılışta bir kez okunup bellekte tutuluyor; rozet üretiminde disk okuması yok.
  * ⚠️ **Ölçekleme dosyanın kendi `viewBox`'ına göre hesaplanıyor.** Game Icons 512, elle çizilenler 24 kullanıyor — sabit bir çarpan kullanılsaydı alev ya nokta kadar kalır ya rozetin dışına taşardı. Doğrulandı: `classic` → `scale(2.4)`, `candle-light` → `scale(0.11)`, ikisi de aynı 57.6px'e oturuyor.
  * **Dayanıklılık:** bozuk veya eksik bir dosya tüm rozet servisini düşürmüyor — o şekil atlanıyor, çizim klasik alevle sürüyor.
* **3 Ağustos 2026, 15:30** - **Rütbe → alev eşleşmesi** (eşikler milestone bildirimleriyle aynı): Kıvılcım `candle-light` · Alev `classic` · Ateş `burning-embers` · Yangın `celebration-fire` · Efsane `volcano`.
  * Üç varyantta (normal/kompakt/max) ve "bulunamadı" rozetinde tek bir `RenderFlame` yardımcısı kullanılıyor; alev mantığı tek yerde.
* **3 Ağustos 2026, 15:40** - **[Arayüz] Rütbe galerisi.** Özelleştirme sayfasında beş rütbe de gösteriliyor: kazanılanlar renkli ve animasyonlu, kazanılmayanlar **soluk + kilitli** ve *"X gün kaldı"* bilgisiyle.
  * **Neden önemli:** Milestone'lar şimdiye kadar yalnızca bildirimde vardı, panelde hiç görünmüyordu. Artık kullanıcı neyi hedeflediğini görüyor.
  * Yeni endpoint: `GET /api/v1/badges/flames/{rank}.svg` — kullanıcıya özel veri içermediği için 24 saat önbelleklenebiliyor.
* **3 Ağustos 2026, 15:45** - **FAZ 14 TAMAMLANDI ✅**
  * `dotnet build` → **0 warning / 0 error**, `dotnet test` → **201/201 passed** (184 → 201, **17 yeni test**)
  * Frontend `tsc --noEmit` → hatasız, `npm run build` → başarılı
  * Önizlemeler `rozet-onizleme/rutbe-*.svg` olarak üretildi (5 rütbe × normal + max).
