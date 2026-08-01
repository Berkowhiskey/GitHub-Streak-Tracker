using StreakTracker.API.Enums;

namespace StreakTracker.API.Models.Badges;

/// <summary>
/// Rozetin cizilmesi icin gereken tum veri. SVG uretimi bu kayittan beslenir;
/// boylece generator veritabanindan tamamen bagimsiz (stateless) kalir.
/// </summary>
/// <param name="Username">GitHub kullanici adi.</param>
/// <param name="CurrentStreak">Devam eden kesintisiz gun sayisi.</param>
/// <param name="LongestStreak">En uzun seri rekoru.</param>
/// <param name="HasCommittedToday">Bugun katki yapilip yapilmadigi.</param>
/// <param name="LastCommitDate">Son katki gunu.</param>
public record BadgeData(
    string Username,
    int CurrentStreak,
    int LongestStreak,
    bool HasCommittedToday,
    DateOnly? LastCommitDate,
    /// <summary>Kullanicinin dil tercihi; rozet adresinde ?lang verilmezse bu kullanilir.</summary>
    AppLanguage Language = AppLanguage.Turkish);

/// <summary>
/// Rozet renk temasi. GitHub README'leri hem acik hem koyu arka planda
/// goruntulenebildigi icin acik/koyu varyantlar sunulur; geri kalanlar
/// gelistiricilerin editorlerinde yaygin kullanilan hazir paletlerdir.
/// </summary>
public enum BadgeTheme
{
    Dark = 0,
    Light = 1,
    Dracula = 2,
    TokyoNight = 3,
    Nord = 4,
    Catppuccin = 5,
}

/// <summary>Rozetin boyut/duzen varyanti.</summary>
public enum BadgeVariant
{
    /// <summary>Tam rozet: seri, rutbe, rekor ve son commit.</summary>
    Full = 0,

    /// <summary>Yalnizca alev ve seri sayisi. README'de yan yana rozet dizenler icin.</summary>
    Compact = 1,
}

/// <summary>
/// Rozetin nasil cizilecegini belirleyen secenekler.
/// <para>
/// Tek tek parametre yerine kayit kullaniliyor: yeni bir gorunum secenegi
/// eklendiginde ISvgBadgeService'in tum imzalari degismek zorunda kalmasin.
/// </para>
/// </summary>
public record BadgeRenderOptions(
    BadgeTheme Theme = BadgeTheme.Dark,
    AppLanguage Language = AppLanguage.Turkish,
    BadgeVariant Variant = BadgeVariant.Full,
    bool Animated = true)
{
    /// <summary>Adresten gelen ham tema adini cozer; taninmayan deger koyu temaya duser.</summary>
    public static BadgeTheme ParseTheme(string? theme) => theme?.ToLowerInvariant() switch
    {
        "light" => BadgeTheme.Light,
        "dracula" => BadgeTheme.Dracula,
        "tokyo-night" or "tokyonight" => BadgeTheme.TokyoNight,
        "nord" => BadgeTheme.Nord,
        "catppuccin" or "mocha" => BadgeTheme.Catppuccin,
        _ => BadgeTheme.Dark,
    };

    public static BadgeVariant ParseVariant(string? variant) =>
        string.Equals(variant, "compact", StringComparison.OrdinalIgnoreCase)
            ? BadgeVariant.Compact
            : BadgeVariant.Full;

    /// <summary>
    /// Animasyon varsayilan olarak aciktir; <c>?animated=false</c> ile kapatilabilir.
    /// (Kullanicinin isletim sistemi "hareketi azalt" diyorsa animasyon zaten
    /// CSS tarafinda devre disi kalir.)
    /// </summary>
    public static bool ParseAnimated(string? animated) =>
        !string.Equals(animated, "false", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(animated, "0", StringComparison.Ordinal);
}

/// <summary>
/// Serinin ulastigi rutbe. Esikler milestone bildirimleriyle ayni (7/30/100/365)
/// tutuldu: kullanici kutlama bildirimini aldiginda rozetinde de karsiligini gorur.
/// </summary>
public enum StreakRank
{
    None = 0,
    Spark = 1,
    Flame = 2,
    Fire = 3,
    Blaze = 4,
    Legend = 5,
}

public static class StreakRankExtensions
{
    public static StreakRank RankFor(int currentStreak) => currentStreak switch
    {
        >= 365 => StreakRank.Legend,
        >= 100 => StreakRank.Blaze,
        >= 30 => StreakRank.Fire,
        >= 7 => StreakRank.Flame,
        >= 1 => StreakRank.Spark,
        _ => StreakRank.None,
    };

    public static string DisplayName(this StreakRank rank, AppLanguage language) =>
        language == AppLanguage.English
            ? rank switch
            {
                StreakRank.Legend => "LEGEND",
                StreakRank.Blaze => "BLAZE",
                StreakRank.Fire => "FIRE",
                StreakRank.Flame => "FLAME",
                StreakRank.Spark => "SPARK",
                _ => string.Empty,
            }
            : rank switch
            {
                StreakRank.Legend => "EFSANE",
                StreakRank.Blaze => "YANGIN",
                StreakRank.Fire => "ATES",
                StreakRank.Flame => "ALEV",
                StreakRank.Spark => "KIVILCIM",
                _ => string.Empty,
            };
}

/// <summary>
/// Bir temanin renk paleti.
/// </summary>
internal sealed record BadgePalette(
    string Background,
    string Border,
    string PrimaryText,
    string MutedText,
    string FlameFrom,
    string FlameTo,
    string InactiveFlame)
{
    public static BadgePalette For(BadgeTheme theme) => theme switch
    {
        BadgeTheme.Light => new BadgePalette(
            Background: "#ffffff",
            Border: "#d0d7de",
            PrimaryText: "#1f2328",
            MutedText: "#59636e",
            FlameFrom: "#ff9500",
            FlameTo: "#e5484d",
            InactiveFlame: "#afb8c1"),

        BadgeTheme.Dracula => new BadgePalette(
            Background: "#282a36",
            Border: "#44475a",
            PrimaryText: "#f8f8f2",
            MutedText: "#6272a4",
            FlameFrom: "#ffb86c",
            FlameTo: "#ff5555",
            InactiveFlame: "#44475a"),

        BadgeTheme.TokyoNight => new BadgePalette(
            Background: "#1a1b26",
            Border: "#292e42",
            PrimaryText: "#c0caf5",
            MutedText: "#565f89",
            FlameFrom: "#ff9e64",
            FlameTo: "#f7768e",
            InactiveFlame: "#414868"),

        BadgeTheme.Nord => new BadgePalette(
            Background: "#2e3440",
            Border: "#3b4252",
            PrimaryText: "#eceff4",
            MutedText: "#81a1c1",
            FlameFrom: "#ebcb8b",
            FlameTo: "#bf616a",
            InactiveFlame: "#4c566a"),

        BadgeTheme.Catppuccin => new BadgePalette(
            Background: "#1e1e2e",
            Border: "#313244",
            PrimaryText: "#cdd6f4",
            MutedText: "#a6adc8",
            FlameFrom: "#fab387",
            FlameTo: "#f38ba8",
            InactiveFlame: "#45475a"),

        _ => new BadgePalette(
            Background: "#0d1117",
            Border: "#30363d",
            PrimaryText: "#e6edf3",
            MutedText: "#8b949e",
            FlameFrom: "#ffa028",
            FlameTo: "#f0483e",
            InactiveFlame: "#484f58")
    };
}
