using StreakTracker.API.Models.Notifications;

namespace StreakTracker.API.Services.Interfaces;

/// <summary>
/// Streak'i tehlikede olan kullanicilara bildirim gonderir.
/// Birincil kanal, gizli repodaki Issue'ya atilan @mention'li yorumdur;
/// GitHub Mobile bunu push bildirimine cevirir.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Belirtilen UTC saatine bildirim ayarlamis tum aktif kullanicilari isler:
    /// streak'lerini tazeler ve bugun commit atmamis olanlara uyari gonderir.
    /// Bir kullanicida olusan hata digerlerini etkilemez.
    /// </summary>
    Task<HourlyNotificationSummary> ProcessHourlyNotificationsAsync(
        int utcHour,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tek bir kullaniciya streak uyarisi gonderir (zamanlanmis tur disinda).
    /// Bugun commit atilmissa veya bugun zaten bildirim gonderilmisse atlar.
    /// </summary>
    Task<NotificationResult> SendStreakWarningAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanicinin kendi tetikledigi test bildirimi. Saat ve mukerrerlik
    /// kontrollerini uygulamaz; kurulumun calistigini dogrulamak icindir.
    /// </summary>
    Task<NotificationResult> SendTestNotificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
