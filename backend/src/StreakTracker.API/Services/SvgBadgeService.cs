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

    /// <summary>
    /// GitHub, README'deki rozetleri &lt;img&gt; olarak isler ve harici font yuklemez.
    /// Bu yuzden yalnizca isletim sistemlerinde hazir bulunan font yigini kullanilir.
    /// </summary>
    private const string FontStack =
        "-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif";

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

    public string GenerateStreakBadge(BadgeData data, BadgeTheme theme, AppLanguage language)
    {
        var palette = BadgePalette.For(theme);
        var labels = LabelsFor(language);
        var username = Escape(data.Username);

        // Serisi olmayan kullanicida alev sonuk gorunur; bugun commit yoksa hafif solar.
        var flameFill = data.CurrentStreak > 0 ? "url(#flameGradient)" : palette.InactiveFlame;
        var flameOpacity = data.CurrentStreak > 0 && !data.HasCommittedToday ? "0.65" : "1";

        var lastCommit = data.LastCommitDate is { } date
            ? Escape(date.ToString("d MMM", labels.Culture))
            : "—";

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}" role="img" aria-label="{username}: {data.CurrentStreak} {labels.StreakLabel}">
              <title>{username} - {data.CurrentStreak} {labels.StreakLabel} ({labels.RecordLabel}: {data.LongestStreak})</title>
              <defs>
                <linearGradient id="flameGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="{palette.FlameFrom}"/>
                  <stop offset="100%" stop-color="{palette.FlameTo}"/>
                </linearGradient>
              </defs>

              <rect x="0.5" y="0.5" width="{Width - 1}" height="{Height - 1}" rx="10" fill="{palette.Background}" stroke="{palette.Border}"/>

              <g transform="translate(30,30) scale(2.4)" opacity="{flameOpacity}">
                <path d="M12 2c0 4-3 5.5-5 8-1.5 1.9-2 3.6-2 5.5C5 19.5 8 22 12 22s7-2.5 7-6.5c0-1.9-.5-3.6-2-5.5-2-2.5-5-4-5-8z" fill="{flameFill}"/>
                <path d="M12 12.5c0 2-1.4 2.8-2.4 4-.7.9-1 1.7-1 2.6 0 1.9 1.5 2.9 3.4 2.9s3.4-1 3.4-2.9c0-.9-.3-1.7-1-2.6-1-1.2-2.4-2-2.4-4z" fill="#ffd75e" opacity="{(data.CurrentStreak > 0 ? "0.95" : "0")}"/>
              </g>

              <text x="102" y="70" font-family="{FontStack}" font-size="42" font-weight="700" fill="{palette.PrimaryText}">{data.CurrentStreak}</text>
              <text x="103" y="90" font-family="{FontStack}" font-size="12" fill="{palette.MutedText}">{labels.StreakLabel}</text>

              <line x1="250" y1="26" x2="250" y2="94" stroke="{palette.Border}" stroke-width="1"/>

              <text x="270" y="34" font-family="{FontStack}" font-size="12" fill="{palette.MutedText}">@{username}</text>

              <text x="270" y="62" font-family="{FontStack}" font-size="11" letter-spacing="0.5" fill="{palette.MutedText}">{labels.RecordLabel}</text>
              <text x="378" y="62" font-family="{FontStack}" font-size="14" font-weight="600" text-anchor="end" fill="{palette.PrimaryText}">{data.LongestStreak} {labels.DayUnit}</text>

              <text x="270" y="86" font-family="{FontStack}" font-size="11" letter-spacing="0.5" fill="{palette.MutedText}">{labels.LastCommitLabel}</text>
              <text x="378" y="86" font-family="{FontStack}" font-size="14" font-weight="600" text-anchor="end" fill="{palette.PrimaryText}">{lastCommit}</text>
            </svg>
            """;
    }

    public string GenerateNotFoundBadge(string username, BadgeTheme theme, AppLanguage language)
    {
        var palette = BadgePalette.For(theme);
        var labels = LabelsFor(language);
        var safeUsername = Escape(username);

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}" role="img" aria-label="{labels.NotFoundTitle}">
              <title>@{safeUsername} {labels.NotFoundDetail}</title>
              <rect x="0.5" y="0.5" width="{Width - 1}" height="{Height - 1}" rx="10" fill="{palette.Background}" stroke="{palette.Border}"/>

              <g transform="translate(30,30) scale(2.4)">
                <path d="M12 2c0 4-3 5.5-5 8-1.5 1.9-2 3.6-2 5.5C5 19.5 8 22 12 22s7-2.5 7-6.5c0-1.9-.5-3.6-2-5.5-2-2.5-5-4-5-8z" fill="{palette.InactiveFlame}"/>
              </g>

              <text x="102" y="58" font-family="{FontStack}" font-size="17" font-weight="600" fill="{palette.PrimaryText}">{labels.NotFoundTitle}</text>
              <text x="102" y="80" font-family="{FontStack}" font-size="12" fill="{palette.MutedText}">@{safeUsername} {labels.NotFoundDetail}</text>
            </svg>
            """;
    }

    public string ComputeETag(BadgeData data, BadgeTheme theme, AppLanguage language)
    {
        // Rozetin gorunumunu etkileyen tum alanlar hash'e girer;
        // biri degismedikce istemci 304 Not Modified alabilir.
        // Dil de dahildir: aksi halde dil degistiginde onbellekteki eski rozet gosterilir.
        var signature = string.Join('|',
            data.Username,
            data.CurrentStreak,
            data.LongestStreak,
            data.HasCommittedToday,
            data.LastCommitDate?.ToString("yyyy-MM-dd") ?? "-",
            theme,
            language);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(signature));

        return $"\"{Convert.ToHexString(hash)[..16].ToLowerInvariant()}\"";
    }

    /// <summary>
    /// Kullanici adi URL'den geldigi icin SVG'ye yazilmadan once XML olarak kacislanir.
    /// Aksi halde ozel karakterler cizimi bozabilir veya icerik enjeksiyonuna yol acabilir.
    /// </summary>
    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;
}
