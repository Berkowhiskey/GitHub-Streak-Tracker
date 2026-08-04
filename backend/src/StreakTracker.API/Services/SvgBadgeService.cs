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

    /// <summary>GitHub README'sinde icerik alani ~850px genisligindedir; max rozet onu doldurur.</summary>
    private const int MaxWidth = 850;
    private const int MaxHeight = 200;

    /// <summary>
    /// GitHub, README'deki rozetleri &lt;img&gt; olarak isler ve harici font yuklemez.
    /// Bu yuzden yalnizca isletim sistemlerinde hazir bulunan font yigini kullanilir.
    /// </summary>
    private const string FontStack =
        "-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif";

    /// <summary>
    /// Alevi cizer. Sekil kullanicinin RUTBESINE gore secilir - secilebilir degil,
    /// kazanilir; boylece rozet seriyi tek bakista anlatan bir isarete donusur.
    /// </summary>
    /// <param name="rank">Kullanicinin rutbesi.</param>
    /// <param name="x">Alevin sol ust kosesinin yatay konumu.</param>
    /// <param name="y">Alevin sol ust kosesinin dikey konumu.</param>
    /// <param name="size">Alevin kaplayacagi kare alanin kenar uzunlugu (px).</param>
    /// <param name="fill">Govde dolgusu (gradyan veya sonuk renk).</param>
    /// <param name="opacity">Grup saydamligi.</param>
    /// <param name="showCore">Ic cekirdek cizilsin mi (serisi olmayan kullanicida gizlenir).</param>
    private static string RenderFlame(
        StreakRank rank, double x, double y, double size, string fill, string opacity, bool showCore)
    {
        var asset = FlameLibrary.For(rank);

        // Kaynak dosyalar farkli boyutlarda geliyor (Game Icons 512, elle cizilenler 24);
        // olcek her zaman dosyanin kendi viewBox'ina gore hesaplanir.
        var scale = size / asset.ViewBoxSize;

        var body = string.Join("\n    ", asset.BodyPaths.Select(p =>
            $"""<path class="flame" d="{p}" fill="{fill}"/>"""));

        var core = showCore && asset.CorePaths.Length > 0
            ? "\n    " + string.Join("\n    ", asset.CorePaths.Select(p =>
                $"""<path class="flame-core" d="{p}" fill="#ffd75e" opacity="0.95"/>"""))
            : string.Empty;

        return $"""
              <g transform="translate({F(x)},{F(y)}) scale({F(scale)})" opacity="{opacity}">
                {body}{core}
              </g>
            """;
    }

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

    public string GenerateStreakBadge(BadgeData data, BadgeRenderOptions options) => options.Variant switch
    {
        BadgeVariant.Compact => RenderCompact(data, options),
        BadgeVariant.Max => RenderMax(data, options),
        _ => RenderFull(data, options),
    };

    // -----------------------------------------------------------------------
    // Tam rozet
    // -----------------------------------------------------------------------

    private static string RenderFull(BadgeData data, BadgeRenderOptions options)
    {
        var palette = options.ResolvePalette();
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

            {RenderFlame(rank, 30, 30, 57.6, flameFill, flameOpacity, hasStreak)}

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
        var palette = options.ResolvePalette();
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

            {RenderFlame(StreakRankExtensions.RankFor(data.CurrentStreak), 14, 13, 26.4, flameFill, flameOpacity, hasStreak)}

              <text x="48" y="34" font-family="{FontStack}" font-size="23" font-weight="700" fill="{palette.PrimaryText}">{data.CurrentStreak}</text>
              <text x="{48 + (CountDigits(data.CurrentStreak) * 14) + 6}" y="34" font-family="{FontStack}" font-size="11" fill="{palette.MutedText}">{labels.StreakLabel}</text>
            </svg>
            """;
    }

    // -----------------------------------------------------------------------
    // Max rozet - README'de bir baslik alanini kaplar
    // -----------------------------------------------------------------------

    private static string RenderMax(BadgeData data, BadgeRenderOptions options)
    {
        var palette = options.ResolvePalette();
        var labels = LabelsFor(options.Language);
        var username = Escape(data.Username);

        var hasStreak = data.CurrentStreak > 0;
        var flameFill = hasStreak ? "url(#flameGradient)" : palette.InactiveFlame;
        var flameOpacity = hasStreak && !data.HasCommittedToday ? "0.65" : "1";

        var lastCommit = data.LastCommitDate is { } date
            ? Escape(date.ToString("d MMMM yyyy", labels.Culture))
            : "—";

        var rank = StreakRankExtensions.RankFor(data.CurrentStreak);
        var rankName = rank == StreakRank.None ? string.Empty : rank.DisplayName(options.Language);

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{MaxWidth}" height="{MaxHeight}" viewBox="0 0 {MaxWidth} {MaxHeight}" role="img" aria-label="{username}: {data.CurrentStreak} {labels.StreakLabel}">
              <title>{username} - {data.CurrentStreak} {labels.StreakLabel} ({labels.RecordLabel}: {data.LongestStreak})</title>
              <defs>
                <linearGradient id="flameGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="{palette.FlameFrom}"/>
                  <stop offset="100%" stop-color="{palette.FlameTo}"/>
                </linearGradient>
                <linearGradient id="numberGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="{palette.PrimaryText}"/>
                  <stop offset="100%" stop-color="{palette.FlameTo}"/>
                </linearGradient>
              </defs>
            {FlameAnimationStyle(options.Animated && hasStreak)}
              <rect x="0.5" y="0.5" width="{MaxWidth - 1}" height="{MaxHeight - 1}" rx="16" fill="{palette.Background}" stroke="{palette.Border}"/>

            {RenderFlame(rank, 56, 52, 110.4, flameFill, flameOpacity, hasStreak)}

              <text x="210" y="112" font-family="{FontStack}" font-size="86" font-weight="800" fill="url(#numberGradient)">{data.CurrentStreak}</text>
              <text x="213" y="142" font-family="{FontStack}" font-size="18" letter-spacing="0.5" fill="{palette.MutedText}">{labels.StreakLabel}</text>
            {MaxRankPill(rankName, palette)}
              <line x1="470" y1="42" x2="470" y2="158" stroke="{palette.Border}" stroke-width="1"/>

              <text x="510" y="62" font-family="{FontStack}" font-size="18" font-weight="600" fill="{palette.PrimaryText}">@{username}</text>

              <text x="510" y="106" font-family="{FontStack}" font-size="13" letter-spacing="1" fill="{palette.MutedText}">{labels.RecordLabel}</text>
              <text x="{MaxWidth - 56}" y="106" font-family="{FontStack}" font-size="22" font-weight="700" text-anchor="end" fill="{palette.PrimaryText}">{data.LongestStreak} {labels.DayUnit}</text>

              <text x="510" y="146" font-family="{FontStack}" font-size="13" letter-spacing="1" fill="{palette.MutedText}">{labels.LastCommitLabel}</text>
              <text x="{MaxWidth - 56}" y="146" font-family="{FontStack}" font-size="22" font-weight="700" text-anchor="end" fill="{palette.PrimaryText}">{lastCommit}</text>
            </svg>
            """;
    }

    /// <summary>Max rozetteki rutbe etiketi; sayinin hemen altinda, genis ve okunakli.</summary>
    private static string MaxRankPill(string rankName, BadgePalette palette)
    {
        if (string.IsNullOrEmpty(rankName))
            return string.Empty;

        var pillWidth = (rankName.Length * 10.5) + 30;

        return $"""
                  <g>
                    <rect x="210" y="26" width="{F(pillWidth)}" height="28" rx="14" fill="url(#flameGradient)" opacity="0.18"/>
                    <text x="{F(210 + (pillWidth / 2))}" y="45" font-family="{FontStack}" font-size="14" font-weight="700" letter-spacing="1.2" text-anchor="middle" fill="{palette.FlameFrom}">{rankName}</text>
                  </g>

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
    // Rutbe galerisi icin tek alev
    // -----------------------------------------------------------------------

    public string GenerateFlamePreview(StreakRank rank, BadgeRenderOptions options, bool locked)
    {
        const int size = 72;
        const double flameSize = 52;

        var palette = options.ResolvePalette();

        // Kazanilmamis rutbe sonuk cizilir: kullanici neyi hedefledigini gorur
        // ama henuz sahip olmadigi belli olur.
        var fill = locked ? palette.InactiveFlame : "url(#flameGradient)";
        var opacity = locked ? "0.45" : "1";

        var offset = (size - flameSize) / 2;

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 {size} {size}" role="img" aria-label="{rank.DisplayName(options.Language)}">
              <title>{rank.DisplayName(options.Language)}</title>
              <defs>
                <linearGradient id="flameGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="{palette.FlameFrom}"/>
                  <stop offset="100%" stop-color="{palette.FlameTo}"/>
                </linearGradient>
              </defs>
            {FlameAnimationStyle(options.Animated && !locked)}
            {RenderFlame(rank, offset, offset, flameSize, fill, opacity, showCore: !locked)}
            </svg>
            """;
    }

    // -----------------------------------------------------------------------
    // Bulunamadi rozeti
    // -----------------------------------------------------------------------

    public string GenerateNotFoundBadge(string username, BadgeRenderOptions options)
    {
        var palette = options.ResolvePalette();
        var labels = LabelsFor(options.Language);
        var safeUsername = Escape(username);

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}" role="img" aria-label="{labels.NotFoundTitle}">
              <title>@{safeUsername} {labels.NotFoundDetail}</title>
              <rect x="0.5" y="0.5" width="{Width - 1}" height="{Height - 1}" rx="10" fill="{palette.Background}" stroke="{palette.Border}"/>

            {RenderFlame(StreakRank.None, 30, 30, 57.6, palette.InactiveFlame, "1", showCore: false)}

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
            options.Animated,
            options.FlameFrom ?? "-",
            options.FlameTo ?? "-",
            options.Background ?? "-",
            options.Border ?? "-");

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
