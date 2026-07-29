using System.Xml.Linq;
using StreakTracker.API.Models.Badges;
using StreakTracker.API.Services;

namespace StreakTracker.Tests;

public class SvgBadgeServiceTests
{
    private readonly SvgBadgeService _service = new();

    private static BadgeData Data(
        string username = "Berkowhiskey",
        int current = 12,
        int longest = 45,
        bool today = true,
        string? lastCommit = "2026-07-28")
        => new(username, current, longest, today,
            lastCommit is null ? null : DateOnly.Parse(lastCommit));

    [Theory]
    [InlineData(BadgeTheme.Dark)]
    [InlineData(BadgeTheme.Light)]
    public void Uretilen_rozet_gecerli_xml_olmalidir(BadgeTheme theme)
    {
        // Gecersiz XML, README'de kirik resim olarak gorunur - bu yuzden ayristirma testi kritik.
        var svg = _service.GenerateStreakBadge(Data(), theme);

        var document = XDocument.Parse(svg);

        Assert.Equal("svg", document.Root!.Name.LocalName);
    }

    [Fact]
    public void Rozet_streak_ve_rekor_degerlerini_icerir()
    {
        var svg = _service.GenerateStreakBadge(Data(current: 12, longest: 45), BadgeTheme.Dark);

        Assert.Contains(">12<", svg);
        Assert.Contains("45 gun", svg);
        Assert.Contains("@Berkowhiskey", svg);
    }

    [Fact]
    public void Temalar_farkli_arka_plan_renkleri_kullanir()
    {
        var dark = _service.GenerateStreakBadge(Data(), BadgeTheme.Dark);
        var light = _service.GenerateStreakBadge(Data(), BadgeTheme.Light);

        Assert.Contains("#0d1117", dark);
        Assert.Contains("#ffffff", light);
        Assert.NotEqual(dark, light);
    }

    [Fact]
    public void Serisi_olmayan_kullanicida_alev_sonuk_cizilir()
    {
        var svg = _service.GenerateStreakBadge(Data(current: 0, longest: 0), BadgeTheme.Dark);

        // Gradient yerine pasif renk kullanilmali.
        Assert.DoesNotContain("fill=\"url(#flameGradient)\"", svg);
        Assert.Contains("#484f58", svg);
    }

    [Fact]
    public void Son_commit_yoksa_tire_gosterilir()
    {
        var svg = _service.GenerateStreakBadge(Data(lastCommit: null), BadgeTheme.Dark);

        Assert.Contains("—", svg);
        XDocument.Parse(svg);
    }

    [Fact]
    public void Kullanici_adindaki_ozel_karakterler_xml_olarak_kacislanir()
    {
        // Kullanici adi URL'den geldigi icin icerik enjeksiyonuna karsi korunmali.
        var svg = _service.GenerateStreakBadge(Data(username: "<script>alert(1)</script>"), BadgeTheme.Dark);

        Assert.DoesNotContain("<script>", svg);
        Assert.Contains("&lt;script&gt;", svg);

        // Kacis dogru yapildiysa belge hala ayristirilabilir olmalidir.
        XDocument.Parse(svg);
    }

    [Fact]
    public void Bulunamadi_rozeti_gecerli_xml_ve_bilgilendirici_olmalidir()
    {
        var svg = _service.GenerateNotFoundBadge("bilinmeyen", BadgeTheme.Light);

        XDocument.Parse(svg);
        Assert.Contains("Kullanici bulunamadi", svg);
        Assert.Contains("@bilinmeyen", svg);
    }

    [Fact]
    public void ETag_ayni_veri_icin_ayni_farkli_veri_icin_farklidir()
    {
        var etag1 = _service.ComputeETag(Data(current: 12), BadgeTheme.Dark);
        var etag2 = _service.ComputeETag(Data(current: 12), BadgeTheme.Dark);
        var etag3 = _service.ComputeETag(Data(current: 13), BadgeTheme.Dark);
        var etag4 = _service.ComputeETag(Data(current: 12), BadgeTheme.Light);

        Assert.Equal(etag1, etag2);
        Assert.NotEqual(etag1, etag3); // streak degisti
        Assert.NotEqual(etag1, etag4); // tema degisti
    }

    [Fact]
    public void ETag_cift_tirnak_icinde_dondurulur()
    {
        // HTTP ETag sozdizimi tirnak ister; aksi halde istemciler degeri yok sayar.
        var etag = _service.ComputeETag(Data(), BadgeTheme.Dark);

        Assert.StartsWith("\"", etag);
        Assert.EndsWith("\"", etag);
    }

    [Fact]
    public void Uc_haneli_streak_degeri_bozulmadan_cizilir()
    {
        var svg = _service.GenerateStreakBadge(Data(current: 365, longest: 365), BadgeTheme.Dark);

        Assert.Contains(">365<", svg);
        XDocument.Parse(svg);
    }
}
