namespace StreakTracker.API.Services;

/// <summary>
/// Kullanicinin kendi saat diliminde "simdi" ve "bugun" hesaplarini yapar.
///
/// <para>
/// <b>Neden gerekli:</b> Streak tamamen gun bazli bir kavramdir ve "gun" herkes icin
/// ayni anda baslamaz. UTC'ye gore hesaplamak, Turkiye'de gece 01:00'da atilan bir
/// commit'in "dune" yazilmasina ve "gun bitmesine 3 saat kaldi" mesajinin yanlis
/// zamanda gitmesine yol acar.
/// </para>
///
/// Saf ve yan etkisizdir; dogrudan test edilebilir.
/// </summary>
public static class UserClock
{
    /// <summary>Saat dilimi cozumlenemezse kullanilan guvenli varsayilan.</summary>
    public const string FallbackTimeZoneId = "UTC";

    /// <summary>
    /// IANA kimligini <see cref="TimeZoneInfo"/> nesnesine cevirir.
    /// Taninmayan veya bos bir kimlik gelirse UTC'ye duser - kullanicinin
    /// bildirimlerini tamamen durdurmaktansa UTC'ye donmek daha guvenlidir.
    /// </summary>
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception)
        {
            // TimeZoneNotFoundException veya InvalidTimeZoneException.
            // Linux'ta tzdata paketi eksikse de buraya duseriz.
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>Verilen saat diliminde su anki yerel zaman.</summary>
    public static DateTime NowIn(TimeZoneInfo timeZone, DateTime? utcNow = null) =>
        TimeZoneInfo.ConvertTimeFromUtc(utcNow ?? DateTime.UtcNow, timeZone);

    /// <summary>Verilen saat diliminde bugunun tarihi.</summary>
    public static DateOnly TodayIn(TimeZoneInfo timeZone, DateTime? utcNow = null) =>
        DateOnly.FromDateTime(NowIn(timeZone, utcNow));

    /// <summary>Verilen saat diliminde su anki saat (0-23).</summary>
    public static int CurrentHourIn(TimeZoneInfo timeZone, DateTime? utcNow = null) =>
        NowIn(timeZone, utcNow).Hour;

    /// <summary>
    /// Kullanicinin gununun bitmesine kalan tam saat sayisi (0-24).
    /// Bildirim mesajindaki "gun bitmesine X saat kaldi" ifadesi icin kullanilir.
    /// </summary>
    public static int HoursLeftInDay(TimeZoneInfo timeZone, DateTime? utcNow = null) =>
        24 - CurrentHourIn(timeZone, utcNow);

    /// <summary>
    /// Kullanicinin gununun basladigi UTC anini dondurur.
    /// "Bugun bildirim gonderildi mi?" gibi sorgular icin gereklidir; aksi halde
    /// UTC gun siniri kullanilir ve saat dilimi farki olan kullanicilarda yanlis sonuc verir.
    /// </summary>
    public static DateTime StartOfTodayUtc(TimeZoneInfo timeZone, DateTime? utcNow = null)
    {
        var localMidnight = TodayIn(timeZone, utcNow).ToDateTime(TimeOnly.MinValue);

        // DST gecisinde var olmayan bir saate denk gelirse ConvertTimeToUtc istisna
        // firlatir; boyle bir durumda gun basini UTC'ye esitlemek yeterince guvenlidir.
        try
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localMidnight, DateTimeKind.Unspecified), timeZone);
        }
        catch (ArgumentException)
        {
            return DateTime.SpecifyKind(localMidnight, DateTimeKind.Utc);
        }
    }
}
