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
    DateOnly? LastCommitDate);

/// <summary>
/// Rozet renk temasi. GitHub README'leri hem acik hem koyu arka planda
/// goruntulenebildigi icin iki varyant sunulur.
/// </summary>
public enum BadgeTheme
{
    Dark = 0,
    Light = 1
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
