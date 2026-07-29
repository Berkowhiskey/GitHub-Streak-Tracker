namespace StreakTracker.API.Services;

/// <summary>
/// Bildirim metinlerini uretir. Saf (yan etkisiz) mantiktir; dogrudan test edilebilir.
/// Mesaj her zaman @mention ile BASLAR - GitHub Mobile push bildiriminde metnin
/// bas kismi gosterildigi icin uyarinin kilit kismi one alinmistir.
/// </summary>
public static class NotificationMessageBuilder
{
    /// <summary>
    /// Streak'i tehlikede olan kullaniciya gonderilen uyari.
    /// </summary>
    public static string BuildStreakWarning(string username, int currentStreak, int longestStreak, int hoursLeft)
    {
        var streakLine = currentStreak > 0
            ? $"**{currentStreak} gunluk** serin var ve bugun henuz commit atmadin."
            : "Bugun henuz commit atmadin. Yeni bir seri baslatmak icin harika bir an!";

        var timeLine = hoursLeft > 0
            ? $"Gun bitmesine **{hoursLeft} saat** kaldi."
            : "Gun bitmek uzere!";

        var recordLine = longestStreak > 0
            ? $"\n\n_Rekorun: {longestStreak} gun_ 🏆"
            : string.Empty;

        return $"""
            @{username} 🔥 **Streak'in tehlikede!**

            {streakLine}
            {timeLine} Bir commit at ve serini koru! 💪{recordLine}
            """;
    }

    /// <summary>
    /// Kullanicinin panelden elle tetikledigi test bildirimi.
    /// Gercek uyaridan acikca ayirt edilebilir olmalidir.
    /// </summary>
    public static string BuildTestNotification(string username, int currentStreak, bool hasCommittedToday)
    {
        var statusLine = hasCommittedToday
            ? "Bugun commit atmissin, serin guvende. ✅"
            : "Bugun henuz commit atmamissin. ⚠️";

        return $"""
            @{username} 🧪 **Test bildirimi**

            Bu bir testtir - StreakTracker bildirimlerinin telefonuna ulastigini dogrulamak icin gonderildi.

            Guncel serin: **{currentStreak} gun**
            {statusLine}

            _Bu mesaji telefonunda gorduysen her sey calisiyor demektir._ 🎉
            """;
    }
}
