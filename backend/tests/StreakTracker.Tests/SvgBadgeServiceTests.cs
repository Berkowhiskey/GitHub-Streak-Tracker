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

    private static BadgeRenderOptions Opts(
        BadgeTheme theme = BadgeTheme.Dark,
        AppLanguage language = AppLanguage.Turkish,
        BadgeVariant variant = BadgeVariant.Full,
        bool animated = true)
        => new(theme, language, variant, animated);

    /// <summary>Testlerin cogunda dil onemsiz; varsayilan Turkce ile cizilir.</summary>
    private string Badge(
        BadgeData data,
        BadgeTheme theme = BadgeTheme.Dark,
        AppLanguage language = AppLanguage.Turkish,
        BadgeVariant variant = BadgeVariant.Full,
        bool animated = true)
        => _service.GenerateStreakBadge(data, Opts(theme, language, variant, animated));

    [Theory]
    [InlineData(BadgeTheme.Dark)]
    [InlineData(BadgeTheme.Light)]
    [InlineData(BadgeTheme.Dracula)]
    [InlineData(BadgeTheme.TokyoNight)]
    [InlineData(BadgeTheme.Nord)]
    [InlineData(BadgeTheme.Catppuccin)]
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

    /// <summary>
    /// Her temanin kendine ozgu bir arka plani olmali; yanlislikla ayni paleti
    /// dondurmek (copy-paste hatasi) boylece yakalanir.
    /// </summary>
    [Fact]
    public void Her_tema_benzersiz_bir_gorunum_uretir()
    {
        var themes = Enum.GetValues<BadgeTheme>();

        var rendered = themes.Select(t => Badge(Data(), t)).ToList();

        Assert.Equal(themes.Length, rendered.Distinct().Count());
    }

    [Theory]
    [InlineData("light", BadgeTheme.Light)]
    [InlineData("dracula", BadgeTheme.Dracula)]
    [InlineData("tokyo-night", BadgeTheme.TokyoNight)]
    [InlineData("tokyonight", BadgeTheme.TokyoNight)]
    [InlineData("nord", BadgeTheme.Nord)]
    [InlineData("catppuccin", BadgeTheme.Catppuccin)]
    [InlineData("DRACULA", BadgeTheme.Dracula)]
    [InlineData("bilinmeyen", BadgeTheme.Dark)]
    [InlineData(null, BadgeTheme.Dark)]
    public void Tema_adi_adresten_dogru_cozulur(string? raw, BadgeTheme expected)
    {
        Assert.Equal(expected, BadgeRenderOptions.ParseTheme(raw));
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
        var svg = _service.GenerateNotFoundBadge("bilinmeyen", Opts(BadgeTheme.Light));

        XDocument.Parse(svg);
        Assert.Contains("Kullanici bulunamadi", svg);
        Assert.Contains("@bilinmeyen", svg);
    }

    [Fact]
    public void ETag_ayni_veri_icin_ayni_farkli_veri_icin_farklidir()
    {
        var etag1 = _service.ComputeETag(Data(current: 12), Opts());
        var etag2 = _service.ComputeETag(Data(current: 12), Opts());
        var etag3 = _service.ComputeETag(Data(current: 13), Opts());
        var etag4 = _service.ComputeETag(Data(current: 12), Opts(BadgeTheme.Light));

        Assert.Equal(etag1, etag2);
        Assert.NotEqual(etag1, etag3); // streak degisti
        Assert.NotEqual(etag1, etag4); // tema degisti
    }

    [Fact]
    public void ETag_cift_tirnak_icinde_dondurulur()
    {
        // HTTP ETag sozdizimi tirnak ister; aksi halde istemciler degeri yok sayar.
        var etag = _service.ComputeETag(Data(), Opts());

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
        var notFound = _service.GenerateNotFoundBadge("kimse", Opts(language: language));

        XDocument.Parse(badge);
        XDocument.Parse(notFound);
    }

    [Fact]
    public void Ingilizce_bulunamadi_rozeti_ingilizce_metin_icerir()
    {
        var svg = _service.GenerateNotFoundBadge("nobody", Opts(language: AppLanguage.English));

        Assert.Contains("User not found", svg);
        Assert.Contains("not registered", svg);
        Assert.DoesNotContain("Kullanici bulunamadi", svg);
    }

    [Fact]
    public void Dil_degisince_ETag_de_degisir()
    {
        // Kritik: dil ETag'e dahil edilmezse tarayici onbellekteki eski dildeki
        // rozeti gostermeye devam eder ve kullanici degisikligi hic gormez.
        var tr = _service.ComputeETag(Data(), Opts(language: AppLanguage.Turkish));
        var en = _service.ComputeETag(Data(), Opts(language: AppLanguage.English));

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

    // ------------------------------------------------------------------
    // Rutbe
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(0, StreakRank.None)]
    [InlineData(1, StreakRank.Spark)]
    [InlineData(6, StreakRank.Spark)]
    [InlineData(7, StreakRank.Flame)]
    [InlineData(29, StreakRank.Flame)]
    [InlineData(30, StreakRank.Fire)]
    [InlineData(99, StreakRank.Fire)]
    [InlineData(100, StreakRank.Blaze)]
    [InlineData(364, StreakRank.Blaze)]
    [InlineData(365, StreakRank.Legend)]
    public void Rutbe_esikleri_milestone_degerleriyle_ayni(int streak, StreakRank expected)
    {
        // Esikler bildirim milestone'lariyla (7/30/100/365) bilincli olarak ayni:
        // kullanici kutlama bildirimini aldiginda rozetinde de karsiligini gormeli.
        Assert.Equal(expected, StreakRankExtensions.RankFor(streak));
    }

    [Fact]
    public void Rutbe_rozette_gorunur()
    {
        var svg = Badge(Data(current: 30));

        Assert.Contains("ATES", svg);
        XDocument.Parse(svg);
    }

    [Fact]
    public void Rutbe_dile_gore_cevrilir()
    {
        var tr = Badge(Data(current: 100), language: AppLanguage.Turkish);
        var en = Badge(Data(current: 100), language: AppLanguage.English);

        Assert.Contains("YANGIN", tr);
        Assert.Contains("BLAZE", en);
        Assert.DoesNotContain("YANGIN", en);
    }

    [Fact]
    public void Serisi_olmayan_kullanicida_rutbe_cizilmez()
    {
        // "Rutbesizsin" demek yerine sessiz kalmak daha iyi.
        var svg = Badge(Data(current: 0, longest: 0));

        Assert.DoesNotContain("KIVILCIM", svg);
        XDocument.Parse(svg);
    }

    // ------------------------------------------------------------------
    // Animasyon
    // ------------------------------------------------------------------

    [Fact]
    public void Animasyon_varsayilan_olarak_aciktir()
    {
        var svg = Badge(Data(current: 5));

        Assert.Contains("@keyframes st-flicker", svg);
        XDocument.Parse(svg);
    }

    [Fact]
    public void Animasyon_kapatilabilir()
    {
        var svg = Badge(Data(current: 5), animated: false);

        Assert.DoesNotContain("@keyframes", svg);
        XDocument.Parse(svg);
    }

    /// <summary>
    /// Isletim sisteminde "hareketi azalt" secili kullanicilarda animasyon durmali.
    /// SMIL yerine CSS kullanilmasinin baslica sebebi budur.
    /// </summary>
    [Fact]
    public void Animasyon_hareket_azaltma_tercihine_saygi_gosterir()
    {
        var svg = Badge(Data(current: 5));

        Assert.Contains("prefers-reduced-motion", svg);
    }

    [Fact]
    public void Serisi_olmayan_kullanicida_alev_animasyonu_calismaz()
    {
        // Sonuk bir alevin titremesi anlamsiz olurdu.
        var svg = Badge(Data(current: 0, longest: 0));

        Assert.DoesNotContain("@keyframes", svg);
    }

    [Fact]
    public void Animasyon_tercihi_ETag_i_degistirir()
    {
        var animated = _service.ComputeETag(Data(), Opts(animated: true));
        var still = _service.ComputeETag(Data(), Opts(animated: false));

        Assert.NotEqual(animated, still);
    }

    // ------------------------------------------------------------------
    // Kompakt varyant
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(AppLanguage.Turkish)]
    [InlineData(AppLanguage.English)]
    public void Kompakt_rozet_gecerli_xml_uretir(AppLanguage language)
    {
        var svg = Badge(Data(), language: language, variant: BadgeVariant.Compact);

        XDocument.Parse(svg);
    }

    [Fact]
    public void Kompakt_rozet_daha_kucuktur_ve_yalnizca_seriyi_gosterir()
    {
        var compact = Badge(Data(current: 12, longest: 45), variant: BadgeVariant.Compact);

        Assert.Contains(">12<", compact);

        // Kompakt surumde rekor ve son commit bilgisi yer almaz.
        Assert.DoesNotContain("REKOR", compact);
        Assert.DoesNotContain("SON COMMIT", compact);

        var document = XDocument.Parse(compact);
        Assert.Equal("190", document.Root!.Attribute("width")!.Value);
    }

    [Fact]
    public void Varyant_degisince_ETag_de_degisir()
    {
        var full = _service.ComputeETag(Data(), Opts(variant: BadgeVariant.Full));
        var compact = _service.ComputeETag(Data(), Opts(variant: BadgeVariant.Compact));

        Assert.NotEqual(full, compact);
    }

    [Theory]
    [InlineData("compact", BadgeVariant.Compact)]
    [InlineData("COMPACT", BadgeVariant.Compact)]
    [InlineData("full", BadgeVariant.Full)]
    [InlineData(null, BadgeVariant.Full)]
    public void Varyant_adi_adresten_dogru_cozulur(string? raw, BadgeVariant expected)
    {
        Assert.Equal(expected, BadgeRenderOptions.ParseVariant(raw));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    [InlineData("0", false)]
    public void Animasyon_parametresi_dogru_cozulur(string? raw, bool expected)
    {
        Assert.Equal(expected, BadgeRenderOptions.ParseAnimated(raw));
    }
}
