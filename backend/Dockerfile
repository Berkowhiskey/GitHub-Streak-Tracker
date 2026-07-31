# ---------------------------------------------------------------------------
# StreakTracker API - production imaji
#
# Cok asamali build: SDK ile derlenir, calisma zamaninda yalnizca ASP.NET
# runtime tasinir. Boylece imaj kucuk kalir ve SDK production'a sizmaz.
#
# Oracle Cloud Ampere A1 ornekleri ARM64'tur; imajlar coklu mimari oldugu icin
# sunucuda "docker compose build" calistirmak yeterlidir.
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Once yalnizca proje dosyasini kopyalayip restore ediyoruz; kaynak kod
# degistiginde bu katman onbellekten gelir ve build hizlanir.
COPY src/StreakTracker.API/StreakTracker.API.csproj src/StreakTracker.API/
RUN dotnet restore src/StreakTracker.API/StreakTracker.API.csproj

COPY src/ src/
RUN dotnet publish src/StreakTracker.API/StreakTracker.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# tzdata: TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul") gibi IANA
# saat dilimi cozumlemeleri bu paket olmadan Linux'ta CALISMAZ. Streak'in
# "bugun"u ve bildirim saati kullanicinin saat dilimine gore hesaplandigi icin
# zorunludur; eksikligi ancak calisma aninda fark edilir.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

# Root olmayan kullanici: container ele gecirilse bile yetki sinirli kalir.
RUN adduser --disabled-password --gecos "" --uid 5678 appuser

COPY --from=build /app/publish .

# DataProtection anahtarlari kalici volume'e yazilir (bkz. docker-compose.prod.yml).
RUN mkdir -p /keys && chown -R appuser:appuser /keys /app

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "StreakTracker.API.dll"]
