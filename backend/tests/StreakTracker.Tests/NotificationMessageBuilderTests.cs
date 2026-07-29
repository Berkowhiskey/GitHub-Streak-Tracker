using StreakTracker.API.Services;

namespace StreakTracker.Tests;

public class NotificationMessageBuilderTests
{
    [Fact]
    public void Uyari_mesaji_mention_ile_baslar()
    {
        // GitHub Mobile push bildiriminde metnin bas kismi gosterilir;
        // @mention basta olmazsa bildirim anlamsiz gorunur.
        var message = NotificationMessageBuilder.BuildStreakWarning("Berkowhiskey", 5, 12, 4);

        Assert.StartsWith("@Berkowhiskey", message);
    }

    [Fact]
    public void Uyari_mesaji_guncel_seriyi_ve_rekoru_icerir()
    {
        var message = NotificationMessageBuilder.BuildStreakWarning("testuser", 7, 30, 3);

        Assert.Contains("7 gunluk", message);
        Assert.Contains("30 gun", message);
        Assert.Contains("3 saat", message);
    }

    [Fact]
    public void Serisi_olmayan_kullaniciya_kirilacak_seri_vaadi_yapilmaz()
    {
        // CurrentStreak=0 iken "0 gunluk serin var" demek sacma olurdu.
        var message = NotificationMessageBuilder.BuildStreakWarning("testuser", 0, 0, 5);

        Assert.DoesNotContain("0 gunluk", message);
        Assert.Contains("Yeni bir seri", message);
    }

    [Fact]
    public void Rekoru_olmayan_kullanicida_rekor_satiri_gosterilmez()
    {
        var message = NotificationMessageBuilder.BuildStreakWarning("testuser", 0, 0, 5);

        Assert.DoesNotContain("Rekorun", message);
    }

    [Fact]
    public void Gun_bitmek_uzereyken_saat_yerine_uyari_metni_gosterilir()
    {
        var message = NotificationMessageBuilder.BuildStreakWarning("testuser", 3, 9, 0);

        Assert.Contains("Gun bitmek uzere", message);
        Assert.DoesNotContain("0 saat", message);
    }

    [Fact]
    public void Test_bildirimi_gercek_uyaridan_ayirt_edilebilir()
    {
        var test = NotificationMessageBuilder.BuildTestNotification("testuser", 3, true);
        var warning = NotificationMessageBuilder.BuildStreakWarning("testuser", 3, 9, 4);

        Assert.Contains("Test bildirimi", test);
        Assert.DoesNotContain("tehlikede", test);
        Assert.Contains("tehlikede", warning);
    }

    [Theory]
    [InlineData(true, "guvende")]
    [InlineData(false, "commit atmamissin")]
    public void Test_bildirimi_gunun_commit_durumunu_dogru_yansitir(bool hasCommitted, string expected)
    {
        var message = NotificationMessageBuilder.BuildTestNotification("testuser", 4, hasCommitted);

        Assert.Contains(expected, message);
    }
}
