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
    string GenerateStreakBadge(BadgeData data, BadgeTheme theme);

    /// <summary>
    /// Kullanici bulunamadiginda gosterilecek rozeti cizer.
    /// README'de kirik resim yerine anlamli bir gorsel cikmasi icin kullanilir.
    /// </summary>
    string GenerateNotFoundBadge(string username, BadgeTheme theme);

    /// <summary>
    /// Rozetin icerigini temsil eden ETag degeri uretir.
    /// Streak degismedigi surece ayni kalir; boylece istemciler 304 alabilir.
    /// </summary>
    string ComputeETag(BadgeData data, BadgeTheme theme);
}
