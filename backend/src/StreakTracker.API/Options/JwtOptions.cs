namespace StreakTracker.API.Options;

/// <summary>
/// appsettings.json icindeki "Jwt" bolumunun karsiligi.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "StreakTracker";

    public string Audience { get; set; } = "StreakTracker.Client";

    /// <summary>Imzalama anahtari. En az 32 karakter olmalidir; asla kaynak koda gomulmez.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryDays { get; set; } = 30;
}
