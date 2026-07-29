namespace StreakTracker.API.Models.Notifications;

/// <summary>
/// Bildirim gonderim denemesinin sonucu.
/// </summary>
/// <param name="Sent">Bildirim gercekten gonderildiyse true.</param>
/// <param name="Reason">Gonderilmediyse nedenini aciklayan, kullaniciya gosterilebilir metin.</param>
public record NotificationResult(bool Sent, string Reason)
{
    public static NotificationResult Success(string reason = "Bildirim gonderildi.") => new(true, reason);

    public static NotificationResult Skipped(string reason) => new(false, reason);
}

/// <summary>
/// Saatlik bildirim turunun ozeti (Hangfire job ciktisi ve loglar icin).
/// </summary>
/// <param name="UtcHour">Islenen UTC saati.</param>
/// <param name="UsersChecked">Bu saate ayarli, kontrol edilen kullanici sayisi.</param>
/// <param name="NotificationsSent">Basariyla gonderilen bildirim sayisi.</param>
/// <param name="Failures">Hata alinan kullanici sayisi.</param>
public record HourlyNotificationSummary(int UtcHour, int UsersChecked, int NotificationsSent, int Failures);
