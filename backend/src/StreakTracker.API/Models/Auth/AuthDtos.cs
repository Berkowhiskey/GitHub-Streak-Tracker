namespace StreakTracker.API.Models.Auth;

/// <summary>
/// Basarili GitHub girisi sonrasi frontend'e donen sonuc.
/// </summary>
/// <param name="Token">API cagrilarinda kullanilacak JWT.</param>
/// <param name="ExpiresAt">JWT'nin gecerlilik bitisi (UTC).</param>
/// <param name="User">Giris yapan kullanicinin ozeti.</param>
/// <param name="RequiresOnboarding">
/// true ise kullanici KVKK/bildirim onayini henuz vermemistir ve
/// gizli repo kurulumu yapilmamistir; frontend onboarding ekranini gostermelidir.
/// </param>
public record AuthResultDto(
    string Token,
    DateTime ExpiresAt,
    CurrentUserDto User,
    bool RequiresOnboarding);

/// <summary>
/// Kullanicinin frontend'de gosterilen profil ve durum ozeti.
/// </summary>
public record CurrentUserDto(
    Guid Id,
    string GitHubUsername,
    string? Email,
    string? AvatarUrl,
    bool HasAcceptedTerms,
    bool IsActive,
    int PreferredNotificationHourUtc,
    string? NotificationRepoName,
    int? NotificationIssueNumber,
    /// <summary>
    /// GitHub App kurulu mu? Kurulu degilse bildirimler gonderilemez,
    /// cunku yorumun bot kimligiyle atilmasi gerekir.
    /// </summary>
    bool GitHubAppInstalled);

/// <summary>
/// GitHub App kurulum durumu ve kurulum adresi.
/// </summary>
public record AppInstallationStatusDto(bool Installed, string InstallationUrl, bool AppConfigured);

/// <summary>
/// Onboarding (KVKK onayi + gizli repo kurulumu) istegi.
/// </summary>
public class OnboardingRequest
{
    /// <summary>
    /// Kullanicinin bilgilendirme metnini okuyup onayladigini belirtir.
    /// false ise kurulum yapilmaz - onay olmadan hesabinda repo olusturmayiz.
    /// </summary>
    public bool AcceptTerms { get; set; }

    /// <summary>Bildirim gonderilecek saat (UTC, 0-23). Belirtilmezse varsayilan korunur.</summary>
    public int? PreferredNotificationHourUtc { get; set; }
}

/// <summary>
/// Onboarding sonucu: kurulan bildirim altyapisi ve ilk streak durumu.
/// </summary>
public record OnboardingResultDto(
    string RepositoryName,
    int IssueNumber,
    bool WasAlreadySetUp,
    int CurrentStreak,
    int LongestStreak,
    bool HasCommittedToday,
    DateOnly? LastCommitDate);
