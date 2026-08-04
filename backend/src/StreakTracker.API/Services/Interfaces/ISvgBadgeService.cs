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
    string GenerateStreakBadge(BadgeData data, BadgeRenderOptions options);

    /// <summary>
    /// Tek bir rutbenin alev seklini kare bir alanda cizer.
    /// Panelde rutbe galerisini gostermek icin kullanilir.
    /// </summary>
    /// <param name="rank">Cizilecek rutbe.</param>
    /// <param name="options">Tema ve renk secenekleri.</param>
    /// <param name="locked">
    /// Rutbe henuz kazanilmadiysa true; alev sonuk ve animasyonsuz cizilir.
    /// </param>
    string GenerateFlamePreview(StreakRank rank, BadgeRenderOptions options, bool locked);

    /// <summary>
    /// Kullanici bulunamadiginda gosterilecek rozeti cizer.
    /// README'de kirik resim yerine anlamli bir gorsel cikmasi icin kullanilir.
    /// </summary>
    string GenerateNotFoundBadge(string username, BadgeRenderOptions options);

    /// <summary>
    /// Rozetin icerigini temsil eden ETag degeri uretir.
    /// Streak degismedigi surece ayni kalir; boylece istemciler 304 alabilir.
    /// <para>
    /// Gorunumu etkileyen her sey (tema, dil, varyant, animasyon) imzaya dahildir:
    /// aksi halde bunlardan biri degistiginde tarayici onbellekteki eski rozeti gosterir.
    /// </para>
    /// </summary>
    string ComputeETag(BadgeData data, BadgeRenderOptions options);
}
