# StreakTracker

GitHub streak takibi, GitHub Mobile üzerinden push bildirimi ve dinamik SVG rozet servisi.
Projenin tam mimarisi, fazları ve ilerleme günlüğü için: [CLAUDE.md](CLAUDE.md)

## Gereksinimler

- .NET 9 SDK
- Docker Desktop
- Node.js 20+ (Faz 6 — frontend için)

## Geliştirme Ortamını Çalıştırma

```bash
# 1) PostgreSQL'i ayağa kaldır (localhost:5434)
docker compose up -d

# 2) Yerel ayar dosyanı oluştur
cp backend/src/StreakTracker.API/appsettings.Development.example.json \
   backend/src/StreakTracker.API/appsettings.Development.json

# 3) API'yi çalıştır
dotnet run --project backend/src/StreakTracker.API --launch-profile http
```

| Adres | Açıklama |
|---|---|
| http://localhost:5157/health | Servis + veritabanı sağlık kontrolü |
| http://localhost:5157/swagger | API dokümantasyonu |
| http://localhost:5157/hangfire | Arka plan görev paneli |

> **Not:** PostgreSQL **5434** portundadır (5432/5433 makinedeki diğer projeler tarafından kullanılıyor).

## Veritabanı Migration

```bash
cd backend/src/StreakTracker.API

dotnet ef migrations add <MigrationAdi> --output-dir Data/Migrations
dotnet ef database update
```

Development ortamında uygulama her açılışta bekleyen migration'ları otomatik uygular.

## Proje Yapısı

```
GitLingo/
├── backend/
│   ├── StreakTracker.sln
│   └── src/StreakTracker.API/
│       ├── Data/               # AppDbContext, Fluent API konfigürasyonları, migration'lar
│       ├── Entities/           # User, Streak, NotificationLog
│       ├── Enums/              # NotificationChannel
│       └── Controllers/        # (Faz 4-5)
├── frontend/                   # (Faz 6 — Next.js)
├── docker-compose.yml
└── CLAUDE.md
```
