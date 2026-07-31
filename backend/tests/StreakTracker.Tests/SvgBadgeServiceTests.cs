using System.Xml.Linq;
using StreakTracker.API.Enums;
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

    /// <summary>Testlerin cogunda dil onemsiz; varsayilan Turkce ile cizilir.</summary>
    private string Badge(BadgeData data, BadgeTheme theme = BadgeTheme.Dark,
        AppLanguage language = AppLanguage.Turkish)
        => _service.GenerateStreakBadge(data, theme, language);

    [Theory]
    [InlineData(BadgeTheme.Dark)]
    [InlineData(BadgeTheme.Light)]
    public void Uretilen_rozet_gecerli_xml_olmalidir(BadgeTheme theme)
    {
        // Gecersiz XML, README'de kirik resim olarak gorunur - bu yuzden ayristirma testi kritik.
        var svg = Badge(Data(), theme);

        var document = XDocument.Parse(svg);

        Assert.Equal("svg", document.Root!.Name.LocalName);
    }

    [Fact]
    public void Rozet_streak_ve_rekor_degerlerini_icerir()
    {
        var svg = Badge(Data(current: 12, longest: 45));

        Assert.Contains(">12<", svg);
        Assert.Contains("45 gun", svg);
        Assert.Contains("@Berkowhiskey", svg);
    }

    [Fact]
    public void Temalar_farkli_arka_plan_renkleri_kullanir()
    {
        var dark = Badge(Data(), BadgeTheme.Dark);
        var light = Badge(Data(), BadgeTheme.Light);

        Assert.Contains("#0d1117", dark);
        Assert.Contains("#ffffff", light);
        Assert.NotEqual(dark, light);
    }

    [Fact]
    public void Serisi_olmayan_kullanicida_alev_sonuk_cizilir()
    {
        var svg = Badge(Data(current: 0, longest: 0));

        // Gradient yerine pasif renk kullanilmali.
        Assert.DoesNotContain("fill=\"url(#flameGradient)\"", svg);
        Assert.Contains("#484f58", svg);
    }

    [Fact]
    public void Son_commit_yoksa_tire_gosterilir()
    {
        var svg = Badge(Data(lastCommit: null));

        Assert.Contains("—", svg);
        XDocument.Parse(svg);
    }

    [Fact]
    public void Kullanici_adindaki_ozel_karakterler_xml_olarak_kacislanir()
    {
        // Kullanici adi URL'den geldigi icin icerik enjeksiyonuna karsi korunmali.
        var svg = Badge(Data(username: "<script>alert(1)</script>"));

        Assert.DoesNotContain("<script>", svg);
        Assert.Contains("&lt;script&gt;", svg);

        // Kacis dogru yapildiysa belge hala ayristirilabilir olmalidir.
        XDocument.Parse(svg);
    }

    [Fact]
    public void Bulunamadi_rozeti_gecerli_xml_ve_bilgilendirici_olmalidir()
    {
        var svg = _service.GenerateNotFoundBadge("bilinmeyen", BadgeTheme.Light, AppLanguage.Turkish);

        XDocument.Parse(svg);
        Assert.Contains("Kullanici bulunamadi", svg);
        Assert.Contains("@bilinmeyen", svg);
    }

    [Fact]
    public void ETag_ayni_veri_icin_ayni_farkli_veri_icin_farklidir()
    {
        var etag1 = _service.ComputeETag(Data(current: 12), BadgeTheme.Dark, AppLanguage.Turkish);
        var etag2 = _service.ComputeETag(Data(current: 12), BadgeTheme.Dark, AppLanguage.Turkish);
        var etag3 = _service.ComputeETag(Data(current: 13), BadgeTheme.Dark, AppLanguage.Turkish);
        var etag4 = _service.ComputeETag(Data(current: 12), BadgeTheme.Light, AppLanguage.Turkish);

        Assert.Equal(etag1, etag2);
        Assert.NotEqual(etag1, etag3); // streak degisti
        Assert.NotEqual(etag1, etag4); // tema degisti
    }

    [Fact]
    public void ETag_cift_tirnak_icinde_dondurulur()
    {
        // HTTP ETag sozdizimi tirnak ister; aksi halde istemciler degeri yok sayar.
        var etag = _service.ComputeETag(Data(), BadgeTheme.Dark, AppLanguage.Turkish);

        Assert.StartsWith("\"", etag);
        Assert.EndsWith("\"", etag);
    }

    [Fact]
    public void Uc_haneli_streak_degeri_bozulmadan_cizilir()
    {
        var svg = Badge(Data(current: 365, longest: 365));

        Assert.Contains(">365<", svg);
        XDocument.Parse(svg);
    }

    // ------------------------------------------------------------------
    // Dil destegi
    // ------------------------------------------------------------------

    [Fact]
    public void Ingilizce_rozet_ingilizce_etiketler_kullanir()
    {
        var svg = Badge(Data(), language: AppLanguage.English);

        Assert.Contains("day streak", svg);
        Assert.Contains("RECORD", svg);
        Assert.Contains("LAST COMMIT", svg);
        Assert.Contains("45 days", svg);

        // Turkce etiketler kalmamali.
        Assert.DoesNotContain("gunluk seri", svg);
        Assert.DoesNotContain("REKOR", svg);
    }

    [Theory]
    [InlineData(AppLanguage.Turkish)]
    [InlineData(AppLanguage.English)]
    public void Her_iki_dilde_de_gecerli_xml_uretilir(AppLanguage language)
    {
        var badge = Badge(Data(), language: language);
        var notFound = _service.GenerateNotFoundBadge("kimse", BadgeTheme.Dark, language);

        XDocument.Parse(badge);
        XDocument.Parse(notFound);
    }

    [Fact]
    public void Ingilizce_bulunamadi_rozeti_ingilizce_metin_icerir()
    {
        var svg = _service.GenerateNotFoundBadge("nobody", BadgeTheme.Dark, AppLanguage.English);

        Assert.Contains("User not found", svg);
        Assert.Contains("not registered", svg);
        Assert.DoesNotContain("Kullanici bulunamadi", svg);
    }

    [Fact]
    public void Dil_degisince_ETag_de_degisir()
    {
        // Kritik: dil ETag'e dahil edilmezse tarayici onbellekteki eski dildeki
        // rozeti gostermeye devam eder ve kullanici degisikligi hic gormez.
        var tr = _service.ComputeETag(Data(), BadgeTheme.Dark, AppLanguage.Turkish);
        var en = _service.ComputeETag(Data(), BadgeTheme.Dark, AppLanguage.English);

        Assert.NotEqual(tr, en);
    }

    [Fact]
    public void Tarih_bicimi_dile_gore_degisir()
    {
        var tr = Badge(Data(lastCommit: "2026-07-28"), language: AppLanguage.Turkish);
        var en = Badge(Data(lastCommit: "2026-07-28"), language: AppLanguage.English);

        Assert.Contains("28 Tem", tr);
        Assert.Contains("28 Jul", en);
    }
}
