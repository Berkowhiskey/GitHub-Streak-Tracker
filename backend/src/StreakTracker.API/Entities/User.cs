namespace StreakTracker.API.Entities;

/// <summary>
/// Platforma GitHub OAuth ile kaydolmus kullanici.
/// Her kullanicinin GitHub hesabinda, bildirim gonderimi icin kullanilan
/// bir gizli (private) repo ve o repo icinde sabit bir Issue bulunur.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>GitHub'in kendi numeric kullanici ID'si. Kullanici adi degisse bile sabit kalir.</summary>
    public long GitHubId { get; set; }

    /// <summary>GitHub kullanici adi (login). Bildirim yorumlarinda @mention icin kullanilir.</summary>
    public string GitHubUsername { get; set; } = null!;

    public string? Email { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>
    /// OAuth ile alinan GitHub access token.
    /// TODO (Faz 5): Veritabanina yazilmadan once DataProtection API ile sifrelenmeli.
    /// </summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>Otomatik olusturulan gizli bildirim reposunun adi (orn. ".streak-tracker-notifications").</summary>
    public string? PrivateNotificationRepoName { get; set; }

    /// <summary>Gizli repodaki sabit bildirim Issue'sunun numarasi. Bildirimler bu Issue'ya yorum olarak dusulur.</summary>
    public int? NotificationIssueNumber { get; set; }

    /// <summary>
    /// Kullanicinin hesabindaki GitHub App kurulum kimligi.
    /// Bildirimler bu kurulum uzerinden bot kimligiyle gonderilir; null ise
    /// kullanici App'i henuz kurmamistir ve bildirim gonderilemez.
    /// </summary>
    public long? GitHubAppInstallationId { get; set; }

    /// <summary>Kullaniciya bildirim gonderilecek saat (UTC, 0-23). Varsayilan 20:00 UTC.</summary>
    public int PreferredNotificationHourUtc { get; set; } = 20;

    /// <summary>Kullanici bildirim almayi durdurdugunda false olur; job'lar bu kullaniciyi atlar.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>KVKK/Onboarding metninin onaylanip onaylanmadigi. Onay yoksa gizli repo olusturulmaz.</summary>
    public bool HasAcceptedTerms { get; set; }

    public DateTime? TermsAcceptedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // --- Navigation properties ---

    /// <summary>Kullanicinin streak durumu (1-1 iliski).</summary>
    public Streak? Streak { get; set; }

    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}
