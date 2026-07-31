namespace StreakTracker.API.Models.Users;

/// <summary>
/// Bildirim tercihleri guncelleme istegi. Yalnizca gonderilen alanlar degistirilir.
/// </summary>
public class UpdatePreferencesRequest
{
    /// <summary>Bildirim saati (0-23), kullanicinin kendi saat diliminde.</summary>
    public int? PreferredNotificationHour { get; set; }

    /// <summary>IANA saat dilimi kimligi (orn. "Europe/Istanbul").</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Dil kodu: "tr" veya "en".</summary>
    public string? Language { get; set; }

    /// <summary>false yapilirsa kullaniciya bildirim gonderilmez.</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Kullanicinin profil README'sine yapistirabilecegi rozet kod parcaciklari.
/// </summary>
/// <param name="BadgeUrl">Rozetin dogrudan adresi.</param>
/// <param name="Markdown">README.md icin hazir Markdown kodu.</param>
/// <param name="Html">HTML sayfalari icin hazir kod.</param>
public record BadgeSnippetsDto(
    string BadgeUrl,
    string BadgeUrlLight,
    string Markdown,
    string Html);

/// <summary>
/// Dashboard'da gosterilen streak ozeti.
/// </summary>
public record StreakStatusDto(
    int CurrentStreak,
    int LongestStreak,
    bool HasCommittedToday,
    DateOnly? LastCommitDate,
    DateTime LastCheckedAt);

/// <summary>
/// Heatmap/takvim gorunumu icin tek bir gun.
/// </summary>
public record CalendarDayDto(DateOnly Date, int ContributionCount);
