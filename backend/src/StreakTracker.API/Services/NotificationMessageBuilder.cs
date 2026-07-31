using StreakTracker.API.Enums;

namespace StreakTracker.API.Services;

/// <summary>
/// Bildirim metinlerini uretir. Saf (yan etkisiz) mantiktir; dogrudan test edilebilir.
///
/// <para>
/// <b>Tasarim kurali:</b> Mesaj her zaman @mention ile BASLAR - GitHub Mobile push
/// bildiriminde metnin yalnizca bas kismi gosterildigi icin uyarinin kilit kismi
/// one alinmistir.
/// </para>
/// </summary>
public static class NotificationMessageBuilder
{
    /// <summary>
    /// Kutlanan kilometre taslari (gun). Yeterince seyrek secildi: her esik
    /// ozel hissettirmeli, bildirim yorgunlugu yaratmamali.
    /// </summary>
    public static readonly int[] MilestoneDays = [7, 30, 100, 365];

    /// <summary>Verilen seri uzunlugu bir kilometre tasina denk geliyor mu?</summary>
    public static bool IsMilestone(int streak) => Array.IndexOf(MilestoneDays, streak) >= 0;

    // -----------------------------------------------------------------------
    // Streak uyarisi
    // -----------------------------------------------------------------------

    /// <summary>
    /// Streak'i tehlikede olan kullaniciya gonderilen uyari.
    /// </summary>
    public static string BuildStreakWarning(
        string username,
        int currentStreak,
        int longestStreak,
        int hoursLeft,
        AppLanguage language = AppLanguage.Turkish)
    {
        return language == AppLanguage.English
            ? BuildStreakWarningEn(username, currentStreak, longestStreak, hoursLeft)
            : BuildStreakWarningTr(username, currentStreak, longestStreak, hoursLeft);
    }

    private static string BuildStreakWarningTr(string username, int currentStreak, int longestStreak, int hoursLeft)
    {
        var streakLine = currentStreak > 0
            ? $"**{currentStreak} gunluk** serin var ve bugun henuz commit atmadin."
            : "Bugun henuz commit atmadin. Yeni bir seri baslatmak icin harika bir an!";

        var timeLine = hoursLeft > 0
            ? $"Gun bitmesine **{hoursLeft} saat** kaldi."
            : "Gun bitmek uzere!";

        var recordLine = longestStreak > 0 ? $"\n\n_Rekorun: {longestStreak} gun_ 🏆" : string.Empty;

        return $"""
            @{username} 🔥 **Streak'in tehlikede!**

            {streakLine}
            {timeLine} Bir commit at ve serini koru! 💪{recordLine}
            """;
    }

    private static string BuildStreakWarningEn(string username, int currentStreak, int longestStreak, int hoursLeft)
    {
        var streakLine = currentStreak > 0
            ? $"You have a **{currentStreak}-day** streak and haven't committed today yet."
            : "You haven't committed today. Perfect moment to start a new streak!";

        var timeLine = hoursLeft > 0
            ? $"**{hoursLeft} hours** left in your day."
            : "Your day is almost over!";

        var recordLine = longestStreak > 0 ? $"\n\n_Your record: {longestStreak} days_ 🏆" : string.Empty;

        return $"""
            @{username} 🔥 **Your streak is at risk!**

            {streakLine}
            {timeLine} Push a commit and keep it alive! 💪{recordLine}
            """;
    }

    // -----------------------------------------------------------------------
    // Kilometre tasi kutlamasi
    // -----------------------------------------------------------------------

    /// <summary>
    /// Kilometre tasina ulasan kullaniciya gonderilen kutlama.
    /// Uyari mesajlarindan farkli olarak bir sey yapmasini istemez; sadece kutlar.
    /// </summary>
    public static string BuildMilestone(
        string username,
        int streak,
        int longestStreak,
        AppLanguage language = AppLanguage.Turkish)
    {
        return language == AppLanguage.English
            ? BuildMilestoneEn(username, streak, longestStreak)
            : BuildMilestoneTr(username, streak, longestStreak);
    }

    private static string BuildMilestoneTr(string username, int streak, int longestStreak)
    {
        var (title, line) = streak switch
        {
            7 => ("Bir haftayi devirdin!", "7 gundur her gun commit atiyorsun."),
            30 => ("Bir aylik seri!", "30 gun boyunca hic ara vermedin."),
            100 => ("100 gun!", "Uc rakamli seriye ulastin - bu is artik aliskanlik."),
            365 => ("Bir yil!", "365 gun kesintisiz. Bu bir efsane."),
            _ => ($"{streak} gunluk seri!", $"{streak} gundur devam ediyorsun.")
        };

        var recordLine = streak >= longestStreak
            ? "\n\n_Bu senin yeni rekorun._ 🏆"
            : $"\n\n_Rekorun: {longestStreak} gun_";

        return $"""
            @{username} 🎉 **{title}**

            {line} Tebrikler!{recordLine}
            """;
    }

    private static string BuildMilestoneEn(string username, int streak, int longestStreak)
    {
        var (title, line) = streak switch
        {
            7 => ("One week down!", "You've committed every day for 7 days."),
            30 => ("A full month!", "30 days without a single break."),
            100 => ("100 days!", "Triple digits - this is a habit now."),
            365 => ("One year!", "365 days uninterrupted. Legendary."),
            _ => ($"{streak}-day streak!", $"You've kept it going for {streak} days.")
        };

        var recordLine = streak >= longestStreak
            ? "\n\n_This is your new record._ 🏆"
            : $"\n\n_Your record: {longestStreak} days_";

        return $"""
            @{username} 🎉 **{title}**

            {line} Congratulations!{recordLine}
            """;
    }

    // -----------------------------------------------------------------------
    // Test bildirimi
    // -----------------------------------------------------------------------

    /// <summary>
    /// Kullanicinin panelden elle tetikledigi test bildirimi.
    /// Gercek uyaridan acikca ayirt edilebilir olmalidir.
    /// </summary>
    public static string BuildTestNotification(
        string username,
        int currentStreak,
        bool hasCommittedToday,
        AppLanguage language = AppLanguage.Turkish)
    {
        if (language == AppLanguage.English)
        {
            var statusEn = hasCommittedToday
                ? "You've committed today, your streak is safe. ✅"
                : "You haven't committed today yet. ⚠️";

            return $"""
                @{username} 🧪 **Test notification**

                This is a test - sent to verify that StreakTracker notifications reach your phone.

                Current streak: **{currentStreak} days**
                {statusEn}

                _If you're seeing this on your phone, everything works._ 🎉
                """;
        }

        var statusTr = hasCommittedToday
            ? "Bugun commit atmissin, serin guvende. ✅"
            : "Bugun henuz commit atmamissin. ⚠️";

        return $"""
            @{username} 🧪 **Test bildirimi**

            Bu bir testtir - StreakTracker bildirimlerinin telefonuna ulastigini dogrulamak icin gonderildi.

            Guncel serin: **{currentStreak} gun**
            {statusTr}

            _Bu mesaji telefonunda gorduysen her sey calisiyor demektir._ 🎉
            """;
    }
}
