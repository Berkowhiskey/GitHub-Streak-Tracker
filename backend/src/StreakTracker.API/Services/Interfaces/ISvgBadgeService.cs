using StreakTracker.API.Enums;
using StreakTracker.API.Models.Badges;

namespace StreakTracker.API.Services.Interfaces;

/// <summary>
/// Streak rozetini SVG olarak uretir.
/// Tamamen stateless'tir: verilen veriden metin uretir, disariya cagri yapmaz.
/// </summary>
public interface ISvgBadgeService
{
    /// <summary>
    /// Kullanicinin streak rozetini cizer.
    /// </summary>
    string GenerateStreakBadge(BadgeData data, BadgeTheme theme, AppLanguage language);

    /// <summary>
    /// Kullanici bulunamadiginda gosterilecek rozeti cizer.
    /// README'de kirik resim yerine anlamli bir gorsel cikmasi icin kullanilir.
    /// </summary>
    string GenerateNotFoundBadge(string username, BadgeTheme theme, AppLanguage language);

    /// <summary>
    /// Rozetin icerigini temsil eden ETag degeri uretir.
    /// Streak degismedigi surece ayni kalir; boylece istemciler 304 alabilir.
    /// <para>
    /// Dil de imzaya dahildir: aksi halde kullanici dilini degistirdiginde
    /// tarayici onbellekteki eski dildeki rozeti gostermeye devam eder.
    /// </para>
    /// </summary>
    string ComputeETag(BadgeData data, BadgeTheme theme, AppLanguage language);
}
