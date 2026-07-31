using StreakTracker.API.Services;

namespace StreakTracker.Tests;

public class UserClockTests
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); // UTC+3
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); // UTC-5/-4

    [Fact]
    public void Gecersiz_saat_dilimi_UTC_ye_duser()
    {
        // Bildirimleri tamamen durdurmaktansa UTC'ye donmek daha guvenlidir.
        Assert.Equal(TimeZoneInfo.Utc, UserClock.Resolve("Boyle/BirYer"));
        Assert.Equal(TimeZoneInfo.Utc, UserClock.Resolve(""));
        Assert.Equal(TimeZoneInfo.Utc, UserClock.Resolve(null));
    }

    [Fact]
    public void Gecerli_IANA_kimligi_cozumlenir()
    {
        var resolved = UserClock.Resolve("Europe/Istanbul");

        Assert.NotEqual(TimeZoneInfo.Utc, resolved);
        Assert.Equal(TimeSpan.FromHours(3), resolved.GetUtcOffset(new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Gece_yarisindan_sonra_atilan_commit_dogru_gune_yazilir()
    {
        // Projenin en kritik senaryosu: Turkiye'de 01:00, UTC'de hala onceki gun.
        // UTC'ye gore hesaplanirsa commit "dune" yazilir ve seri yanlis gorunur.
        var utcNow = new DateTime(2026, 7, 30, 22, 30, 0, DateTimeKind.Utc); // Istanbul: 31 Tem 01:30

        Assert.Equal(new DateOnly(2026, 7, 31), UserClock.TodayIn(Istanbul, utcNow));
        Assert.Equal(new DateOnly(2026, 7, 30), UserClock.TodayIn(TimeZoneInfo.Utc, utcNow));
    }

    [Fact]
    public void Aksam_saatlerinde_bati_yarikuresi_hala_onceki_gunde_olabilir()
    {
        var utcNow = new DateTime(2026, 7, 31, 02, 0, 0, DateTimeKind.Utc); // New York: 30 Tem 22:00

        Assert.Equal(new DateOnly(2026, 7, 30), UserClock.TodayIn(NewYork, utcNow));
        Assert.Equal(new DateOnly(2026, 7, 31), UserClock.TodayIn(TimeZoneInfo.Utc, utcNow));
    }

    [Fact]
    public void Yerel_saat_dogru_hesaplanir()
    {
        var utcNow = new DateTime(2026, 7, 31, 17, 0, 0, DateTimeKind.Utc);

        Assert.Equal(20, UserClock.CurrentHourIn(Istanbul, utcNow)); // UTC+3
        Assert.Equal(17, UserClock.CurrentHourIn(TimeZoneInfo.Utc, utcNow));
    }

    [Fact]
    public void Gun_sonuna_kalan_saat_kullanicinin_gunune_gore_hesaplanir()
    {
        var utcNow = new DateTime(2026, 7, 31, 17, 0, 0, DateTimeKind.Utc);

        // Istanbul'da saat 20:00 -> 4 saat kaldi. UTC'de 17:00 -> 7 saat.
        Assert.Equal(4, UserClock.HoursLeftInDay(Istanbul, utcNow));
        Assert.Equal(7, UserClock.HoursLeftInDay(TimeZoneInfo.Utc, utcNow));
    }

    [Fact]
    public void Gunun_baslangici_UTC_olarak_dogru_dondurulur()
    {
        // 31 Temmuz 01:30 Istanbul -> o gunun baslangici 30 Temmuz 21:00 UTC.
        var utcNow = new DateTime(2026, 7, 30, 22, 30, 0, DateTimeKind.Utc);

        var startUtc = UserClock.StartOfTodayUtc(Istanbul, utcNow);

        Assert.Equal(new DateTime(2026, 7, 30, 21, 0, 0, DateTimeKind.Utc), startUtc);
        Assert.True(startUtc <= utcNow, "Gun baslangici simdiden sonra olamaz.");
    }

    [Fact]
    public void Yaz_saati_gecisinde_dogru_ofset_kullanilir()
    {
        // New York kisin UTC-5, yazin UTC-4. Sabit ofset varsayilsaydi
        // bildirimler yilin yarisinda bir saat kayardi.
        var kis = new DateTime(2026, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var yaz = new DateTime(2026, 7, 15, 17, 0, 0, DateTimeKind.Utc);

        Assert.Equal(12, UserClock.CurrentHourIn(NewYork, kis)); // UTC-5
        Assert.Equal(13, UserClock.CurrentHourIn(NewYork, yaz)); // UTC-4
    }

    [Fact]
    public void UTC_kullanicilari_icin_davranis_degismez()
    {
        // Saat dilimi secmemis mevcut kullanicilar (varsayilan "UTC") eskisi gibi calismali.
        var utcNow = new DateTime(2026, 7, 31, 20, 0, 0, DateTimeKind.Utc);
        var utc = UserClock.Resolve("UTC");

        Assert.Equal(DateOnly.FromDateTime(utcNow), UserClock.TodayIn(utc, utcNow));
        Assert.Equal(utcNow.Hour, UserClock.CurrentHourIn(utc, utcNow));
        Assert.Equal(24 - utcNow.Hour, UserClock.HoursLeftInDay(utc, utcNow));
    }
}
