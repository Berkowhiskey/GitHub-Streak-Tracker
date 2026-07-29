using StreakTracker.API.Models.GitHub;
using StreakTracker.API.Services;

namespace StreakTracker.Tests;

public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 28);

    /// <summary>Bugunden geriye dogru, belirtilen gunlerde katki olan bir takvim uretir.</summary>
    private static List<ContributionDay> Calendar(params int[] daysAgoWithContribution)
    {
        var active = daysAgoWithContribution.ToHashSet();

        return Enumerable.Range(0, 400)
            .Select(daysAgo => new ContributionDay(
                Today.AddDays(-daysAgo),
                active.Contains(daysAgo) ? 3 : 0))
            .ToList();
    }

    [Fact]
    public void Hic_katki_yoksa_tum_degerler_sifir_doner()
    {
        var result = StreakCalculator.Calculate(Calendar(), Today);

        Assert.Equal(0, result.CurrentStreak);
        Assert.Equal(0, result.LongestStreak);
        Assert.Null(result.LastCommitDate);
        Assert.False(result.HasCommittedToday);
    }

    [Fact]
    public void Bugun_dahil_kesintisiz_seri_dogru_sayilir()
    {
        // Bugun, dun, evvelsi gun... toplam 5 gun.
        var result = StreakCalculator.Calculate(Calendar(0, 1, 2, 3, 4), Today);

        Assert.Equal(5, result.CurrentStreak);
        Assert.Equal(5, result.LongestStreak);
        Assert.True(result.HasCommittedToday);
        Assert.Equal(Today, result.LastCommitDate);
    }

    [Fact]
    public void Bugun_commit_yoksa_seri_kirilmis_sayilmaz_gun_henuz_bitmedi()
    {
        // Bu, bildirim motorunun dayandigi en kritik kural:
        // kullanici bugun henuz commit atmadi ama serisi hala ayakta ve tehlikede.
        var result = StreakCalculator.Calculate(Calendar(1, 2, 3), Today);

        Assert.Equal(3, result.CurrentStreak);
        Assert.False(result.HasCommittedToday);
        Assert.Equal(Today.AddDays(-1), result.LastCommitDate);
    }

    [Fact]
    public void Ne_bugun_ne_dun_katki_varsa_seri_sifirlanir()
    {
        // Dun de commit yok -> seri gercekten kirildi.
        var result = StreakCalculator.Calculate(Calendar(2, 3, 4), Today);

        Assert.Equal(0, result.CurrentStreak);
        Assert.False(result.HasCommittedToday);
        Assert.Equal(Today.AddDays(-2), result.LastCommitDate);
    }

    [Fact]
    public void Gecmisteki_rekor_seri_guncel_seriden_bagimsiz_bulunur()
    {
        // Guncel seri 2 gun; 50 gun once 10 gunluk bir rekor var.
        var days = Calendar(0, 1, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59);

        var result = StreakCalculator.Calculate(days, Today);

        Assert.Equal(2, result.CurrentStreak);
        Assert.Equal(10, result.LongestStreak);
    }

    [Fact]
    public void Tek_gunluk_katki_bir_gunluk_seri_sayilir()
    {
        var result = StreakCalculator.Calculate(Calendar(0), Today);

        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(1, result.LongestStreak);
        Assert.True(result.HasCommittedToday);
    }

    [Fact]
    public void Sifir_katkili_gunler_seriyi_bolmelidir()
    {
        // 0 katki iceren gunler HasContribution=false olmali ve seriyi kirmali.
        var days = new List<ContributionDay>
        {
            new(Today, 1),
            new(Today.AddDays(-1), 0),
            new(Today.AddDays(-2), 5)
        };

        var result = StreakCalculator.Calculate(days, Today);

        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(1, result.LongestStreak);
    }

    [Fact]
    public void Sirasiz_ve_tekrarli_girisler_dogru_islenir()
    {
        var days = new List<ContributionDay>
        {
            new(Today.AddDays(-2), 1),
            new(Today, 4),
            new(Today.AddDays(-1), 2),
            new(Today, 4) // ayni gun tekrar
        };

        var result = StreakCalculator.Calculate(days, Today);

        Assert.Equal(3, result.CurrentStreak);
        Assert.Equal(3, result.LongestStreak);
    }

    [Fact]
    public void Gelecek_tarihli_gunler_hesaba_katilmaz()
    {
        // GitHub takvimi icinde bulunulan haftanin tamamini dondurur;
        // henuz gelmemis gunler seriyi sisirmemeli.
        var days = new List<ContributionDay>
        {
            new(Today.AddDays(2), 9),
            new(Today.AddDays(1), 9),
            new(Today, 1)
        };

        var result = StreakCalculator.Calculate(days, Today);

        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(1, result.LongestStreak);
        Assert.Equal(Today, result.LastCommitDate);
    }

    [Fact]
    public void Ay_ve_yil_sinirlari_asilirken_seri_bozulmaz()
    {
        // 1 Ocak 2026'da, 2025'in son gunlerinden gelen bir seri.
        var newYear = new DateOnly(2026, 1, 1);

        var days = new List<ContributionDay>
        {
            new(newYear, 1),
            new(new DateOnly(2025, 12, 31), 1),
            new(new DateOnly(2025, 12, 30), 1)
        };

        var result = StreakCalculator.Calculate(days, newYear);

        Assert.Equal(3, result.CurrentStreak);
        Assert.Equal(3, result.LongestStreak);
    }
}
