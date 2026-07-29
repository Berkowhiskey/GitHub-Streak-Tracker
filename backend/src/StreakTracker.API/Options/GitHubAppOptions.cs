namespace StreakTracker.API.Options;

/// <summary>
/// appsettings.json icindeki "GitHubApp" bolumunun karsiligi.
///
/// GitHub App, bildirimleri kullanicinin kendisi yerine ayri bir bot kimligi
/// (<c>uygulamaadi[bot]</c>) olarak gonderebilmemizi saglar. GitHub, kullanicinin
/// kendi eylemleri icin bildirim uretmedigi icin bu ayrim zorunludur.
/// </summary>
public class GitHubAppOptions
{
    public const string SectionName = "GitHubApp";

    /// <summary>GitHub App ayarlar sayfasindaki sayisal "App ID".</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// App icin uretilen RSA private key (PEM icerigi).
    /// Bu bir sirdir; kaynak koda veya git'e girmemelidir.
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Private key dosyasinin yolu (.pem). PEM icerigini JSON'a tasimak zahmetli
    /// oldugu icin dosya yolu vermek pratik alternatiftir. Doluysa
    /// <see cref="PrivateKey"/> yerine bu dosya okunur.
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// App'in URL kisaltmasi (orn. "streaktracker-dev").
    /// Kullaniciyi kurulum sayfasina yonlendirmek icin kullanilir.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>App yapilandirilmis mi? Degilse bildirimler gonderilemez.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AppId) &&
        (!string.IsNullOrWhiteSpace(PrivateKey) || File.Exists(PrivateKeyPath));

    /// <summary>
    /// Private key icerigini dondurur: once dosya yolu, yoksa dogrudan verilen icerik.
    /// </summary>
    public string ResolvePrivateKey() =>
        File.Exists(PrivateKeyPath) ? File.ReadAllText(PrivateKeyPath) : PrivateKey;

    /// <summary>Kullanicinin App'i kuracagi GitHub adresi.</summary>
    public string InstallationUrl =>
        string.IsNullOrWhiteSpace(Slug)
            ? "https://github.com/settings/installations"
            : $"https://github.com/apps/{Slug}/installations/new";
}
