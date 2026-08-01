using System.Globalization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using StreakTracker.API.Enums;
using StreakTracker.API.Models.Badges;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="ISvgBadgeService" />
public class SvgBadgeService : ISvgBadgeService
{
    private const int Width = 400;
    private const int Height = 120;

    private const int CompactWidth = 190;
    private const int CompactHeight = 52;

    /// <summary>
    /// GitHub, README'deki rozetleri &lt;img&gt; olarak isler ve harici font yuklemez.
    /// Bu yuzden yalnizca isletim sistemlerinde hazir bulunan font yigini kullanilir.
    /// </summary>
    private const string FontStack =
        "-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif";

    /// <summary>Alevin dis hatti (24x24 kutuda cizilmis yol).</summary>
    private const string FlameOuterPath =
        "M12 2c0 4-3 5.5-5 8-1.5 1.9-2 3.6-2 5.5C5 19.5 8 22 12 22s7-2.5 7-6.5c0-1.9-.5-3.6-2-5.5-2-2.5-5-4-5-8z";

    /// <summary>Alevin ic (sicak) cekirdegi.</summary>
    private const string FlameCorePath =
        "M12 12.5c0 2-1.4 2.8-2.4 4-.7.9-1 1.7-1 2.6 0 1.9 1.5 2.9 3.4 2.9s3.4-1 3.4-2.9c0-.9-.3-1.7-1-2.6-1-1.2-2.4-2-2.4-4z";

    /// <summary>Ay kisaltmalari sunucunun yerel ayarindan bagimsiz olmalidir.</summary>
    private static readonly CultureInfo TurkishCulture = new("tr-TR");
    private static readonly CultureInfo EnglishCulture = new("en-US");

    /// <summary>Rozet uzerindeki sabit etiketler.</summary>
    private sealed record BadgeLabels(
        string StreakLabel,
        string RecordLabel,
        string LastCommitLabel,
        string DayUnit,
        string NotFoundTitle,
        string NotFoundDetail,
        CultureInfo Culture);

    private static BadgeLabels LabelsFor(AppLanguage language) => language == AppLanguage.English
        ? new BadgeLabels("day streak", "RECORD", "LAST COMMIT", "days",
            "User not found", "is not registered on StreakTracker", EnglishCulture)
        : new BadgeLabels("gunluk seri", "REKOR", "SON COMMIT", "gun",
            "Kullanici bulunamadi", "StreakTracker'a kayitli degil", TurkishCulture);

    public string GenerateStreakBadge(BadgeData data, BadgeRenderOptions options)
    {
        return options.Variant == BadgeVariant.Compact
            ? RenderCompact(data, options)
            : RenderFull(data, options);
    }

    // -----------------------------------------------------------------------
    // Tam rozet
    // -----------------------------------------------------------------------

    private static string RenderFull(BadgeData data, BadgeRenderOptions options)
    {
        var palette = BadgePalette.For(options.Theme);
        var labels = LabelsFor(options.Language);
        var username = Escape(data.Username);

        var hasStreak = data.CurrentStreak > 0;

        // Serisi olmayan kullanicida alev sonuk gorunur; bugun commit yoksa hafif solar.
        var flameFill = hasStreak ? "url(#flameGradient)" : palette.InactiveFlame;
        var flameOpacity = hasStreak && !data.HasCommittedToday ? "0.65" : "1";

        var lastCommit = data.LastCommitDate is { } date
            ? Escape(date.ToString("d MMM", labels.Culture))
            : "—";

        var rank = StreakRankExtensions.RankFor(data.CurrentStreak);

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}" role="img" aria-label="{username}: {data.CurrentStreak} {labels.StreakLabel}">
              <title>{username} - {data.CurrentStreak} {labels.StreakLabel} ({labels.RecordLabel}: {data.LongestStreak})</title>
              <defs>
                <linearGradient id="flameGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="{palette.FlameFrom}"/>
                  <stop offset="100%" stop-color="{palette.FlameTo}"/>
                </linearGradient>
              </defs>
            {FlameAnimationStyle(options.Animated && hasStreak)}
              <rect x="0.5" y="0.5" width="{Width - 1}" height="{Height - 1}" rx="10" fill="{palette.Background}" stroke="{palette.Border}"/>

              <g transform="translate(30,30) scale(2.4)" opacity="{flameOpacity}">
                <path class="flame" d="{FlameOuterPath}" fill="{flameFill}"/>
                <path class="flame-core" d="{FlameCorePath}" fill="#ffd75e" opacity="{(hasStreak ? "0.95" : "0")}"/>
              </g>

              <text x="102" y="70" font-family="{FontStack}" font-size="42" font-weight="700" fill="{palette.PrimaryText}">{data.CurrentStreak}</text>
              <text x="103" y="90" font-family="{FontStack}" font-size="12" fill="{palette.MutedText}">{labels.StreakLabel}</text>
            {RankPill(rank, options.Language, palette)}
              <line x1="250" y1="26" x2="250" y2="94" stroke="{palette.Border}" stroke-width="1"/>

              <text x="270" y="34" font-family="{FontStack}" font-size="12" fill="{palette.MutedText}">@{username}</text>

              <text x="270" y="62" font-family="{FontStack}" font-size="11" letter-spacing="0.5" fill="{palette.MutedText}">{labels.RecordLabel}</text>
              <text x="378" y="62" font-family="{FontStack}" font-size="14" font-weight="600" text-anchor="end" fill="{palette.PrimaryText}">{data.LongestStreak} {labels.DayUnit}</text>

              <text x="270" y="86" font-family="{FontStack}" font-size="11" letter-spacing="0.5" fill="{palette.MutedText}">{labels.LastCommitLabel}</text>
              <text x="378" y="86" font-family="{FontStack}" font-size="14" font-weight="600" text-anchor="end" fill="{palette.PrimaryText}">{lastCommit}</text>
            </svg>
            """;
    }

    /// <summary>
    /// Rutbe etiketi. Serisi olmayan kullanicida hic cizilmez - "rutbesizsin"
    /// demek yerine sessiz kalmak daha iyi.
    /// </summary>
    private static string RankPill(StreakRank rank, AppLanguage language, BadgePalette palette)
    {
        if (rank == StreakRank.None)
            return string.Empty;

        var name = rank.DisplayName(language);

        // Metin genisligi kabaca hesaplanir: harici font yuklenemedigi icin
        // gercek olcum yapilamaz, bu yuzden guvenli bir ust sinir kullanilir.
        var pillWidth = (name.Length * 7.0) + 18;
        var pillX = 244 - pillWidth;

        return $"""
                  <g>
                    <rect x="{F(pillX)}" y="46" width="{F(pillWidth)}" height="19" rx="9.5" fill="url(#flameGradient)" opacity="0.16"/>
                    <text x="{F(pillX + (pillWidth / 2))}" y="59.5" font-family="{FontStack}" font-size="10" font-weight="700" letter-spacing="0.8" text-anchor="middle" fill="{palette.FlameFrom}">{name}</text>
                  </g>

            """;
    }

    // -----------------------------------------------------------------------
    // Kompakt rozet
    // -----------------------------------------------------------------------

    private static string RenderCompact(BadgeData data, BadgeRenderOptions options)
    {
        var palette = BadgePalette.For(options.Theme);
        var labels = LabelsFor(options.Language);
        var username = Escape(data.Username);

        var hasStreak = data.CurrentStreak > 0;
        var flameFill = hasStreak ? "url(#flameGradient)" : palette.InactiveFlame;
        var flameOpacity = hasStreak && !data.HasCommittedToday ? "0.65" : "1";

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{CompactWidth}" height="{CompactHeight}" viewBox="0 0 {CompactWidth} {CompactHeight}" role="img" aria-label="{username}: {data.CurrentStreak} {labels.StreakLabel}">
              <title>{username} - {data.CurrentStreak} {labels.StreakLabel}</title>
              <defs>
                <linearGradient id="flameGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="{palette.FlameFrom}"/>
                  <stop offset="100%" stop-color="{palette.FlameTo}"/>
                </linearGradient>
              </defs>
            {FlameAnimationStyle(options.Animated && hasStreak)}
              <rect x="0.5" y="0.5" width="{CompactWidth - 1}" height="{CompactHeight - 1}" rx="8" fill="{palette.Background}" stroke="{palette.Border}"/>

              <g transform="translate(14,13) scale(1.1)" opacity="{flameOpacity}">
                <path class="flame" d="{FlameOuterPath}" fill="{flameFill}"/>
                <path class="flame-core" d="{FlameCorePath}" fill="#ffd75e" opacity="{(hasStreak ? "0.95" : "0")}"/>
              </g>

              <text x="48" y="34" font-family="{FontStack}" font-size="23" font-weight="700" fill="{palette.PrimaryText}">{data.CurrentStreak}</text>
              <text x="{48 + (CountDigits(data.CurrentStreak) * 14) + 6}" y="34" font-family="{FontStack}" font-size="11" fill="{palette.MutedText}">{labels.StreakLabel}</text>
            </svg>
            """;
    }

    // -----------------------------------------------------------------------
    // Animasyon
    // -----------------------------------------------------------------------

    /// <summary>
    /// Alevin "nefes almasini" saglayan CSS animasyonu.
    /// <para>
    /// Neden CSS (SMIL degil): GitHub rozetleri &lt;img&gt; olarak isler; bu baglamda
    /// JavaScript calismaz ama CSS animasyonlari calisir. Ayrica CSS,
    /// <c>prefers-reduced-motion</c> destegi verir - isletim sisteminde "hareketi azalt"
    /// secili kullanicilarda animasyon kendiliginden durur.
    /// </para>
    /// <para>
    /// Yalnizca opacity oynatiliyor: <c>transform</c> tabanli bir titreme, alevin
    /// donusum merkezi grup icinde olceklendigi icin kaymasina yol acardi.
    /// </para>
    /// </summary>
    private static string FlameAnimationStyle(bool animated)
    {
        if (!animated)
            return string.Empty;

        return """
                 <style>
                    .flame { animation: st-flicker 2.8s ease-in-out infinite; }
                    .flame-core { animation: st-core 1.9s ease-in-out infinite; }
                    @keyframes st-flicker { 0%, 100% { opacity: 1; } 50% { opacity: 0.82; } }
                    @keyframes st-core { 0%, 100% { opacity: 0.95; } 45% { opacity: 0.55; } }
                    @media (prefers-reduced-motion: reduce) {
                      .flame, .flame-core { animation: none; }
                    }
                  </style>

            """;
    }

    // -----------------------------------------------------------------------
    // Bulunamadi rozeti
    // -----------------------------------------------------------------------

    public string GenerateNotFoundBadge(string username, BadgeRenderOptions options)
    {
        var palette = BadgePalette.For(options.Theme);
        var labels = LabelsFor(options.Language);
        var safeUsername = Escape(username);

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}" role="img" aria-label="{labels.NotFoundTitle}">
              <title>@{safeUsername} {labels.NotFoundDetail}</title>
              <rect x="0.5" y="0.5" width="{Width - 1}" height="{Height - 1}" rx="10" fill="{palette.Background}" stroke="{palette.Border}"/>

              <g transform="translate(30,30) scale(2.4)">
                <path d="{FlameOuterPath}" fill="{palette.InactiveFlame}"/>
              </g>

              <text x="102" y="58" font-family="{FontStack}" font-size="17" font-weight="600" fill="{palette.PrimaryText}">{labels.NotFoundTitle}</text>
              <text x="102" y="80" font-family="{FontStack}" font-size="12" fill="{palette.MutedText}">@{safeUsername} {labels.NotFoundDetail}</text>
            </svg>
            """;
    }

    public string ComputeETag(BadgeData data, BadgeRenderOptions options)
    {
        // Rozetin gorunumunu etkileyen tum alanlar hash'e girer;
        // biri degismedikce istemci 304 Not Modified alabilir.
        // Tema, dil, varyant ve animasyon da dahildir: aksi halde bunlardan biri
        // degistiginde tarayici onbellekteki eski rozeti gostermeye devam eder.
        var signature = string.Join('|',
            data.Username,
            data.CurrentStreak,
            data.LongestStreak,
            data.HasCommittedToday,
            data.LastCommitDate?.ToString("yyyy-MM-dd") ?? "-",
            options.Theme,
            options.Language,
            options.Variant,
            options.Animated);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(signature));

        return $"\"{Convert.ToHexString(hash)[..16].ToLowerInvariant()}\"";
    }

    private static int CountDigits(int value) => value == 0 ? 1 : (int)Math.Floor(Math.Log10(value)) + 1;

    /// <summary>Ondalik ayirici sunucunun yerel ayarina gore degismemelidir.</summary>
    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Kullanici adi URL'den geldigi icin SVG'ye yazilmadan once XML olarak kacislanir.
    /// Aksi halde ozel karakterler cizimi bozabilir veya icerik enjeksiyonuna yol acabilir.
    /// </summary>
    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;
}
