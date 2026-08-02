using System.Xml.Linq;
using StreakTracker.API.Enums;
using StreakTracker.API.Models.Badges;
using StreakTracker.API.Services;

namespace StreakTracker.Tests;

/// <summary>
/// Rozet gorunum ayarlari: saklama, imza ve ozel renkler.
/// Renk dogrulamasi guvenlik acisindan kritiktir - kullanicidan gelen deger
/// dogrudan SVG metnine yaziliyor.
/// </summary>
public class BadgeSettingsTests
{
    private readonly SvgBadgeService _service = new();

    private static BadgeData Data(int current = 12) =>
        new("Berkowhiskey", current, 45, true, new DateOnly(2026, 7, 28));

    // ------------------------------------------------------------------
    // Renk dogrulama - guvenlik
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("#fff")]
    [InlineData("#FFF")]
    [InlineData("#ff9500")]
    [InlineData("#FF9500")]
    public void Gecerli_hex_renkler_kabul_edilir(string color)
    {
        Assert.True(BadgeSettings.IsValidColor(color));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("red")]                        // isim ile renk
    [InlineData("ff9500")]                     // # yok
    [InlineData("#ff95")]                      // gecersiz uzunluk
    [InlineData("#ff95000")]                   // gecersiz uzunluk
    [InlineData("#gggggg")]                    // hex disi karakter
    [InlineData("url(#x)")]
    [InlineData("#fff\" onload=\"alert(1)")]   // oznitelik kacisi denemesi
    [InlineData("#fff\"/><script>x</script>")] // eleman enjeksiyonu denemesi
    public void Gecersiz_renkler_reddedilir(string? color)
    {
        Assert.False(BadgeSettings.IsValidColor(color));
    }

    /// <summary>
    /// Enjeksiyon denemesi rozete hicbir yoldan sizmamali. Renk kacislanmiyor,
    /// bastan reddediliyor: desene uymayan deger temanin rengine duser.
    /// </summary>
    [Fact]
    public void Zararli_renk_denemesi_SVG_e_sizmaz()
    {
        var settings = new BadgeSettings
        {
            Background = "#000\"/><script>alert(1)</script><rect fill=\"#000",
            FlameFrom = "#fff\" onload=\"alert(1)",
        }.Sanitized();

        Assert.Null(settings.Background);
        Assert.Null(settings.FlameFrom);

        var svg = _service.GenerateStreakBadge(
            Data(), settings.ToRenderOptions(AppLanguage.Turkish));

        Assert.DoesNotContain("<script>", svg);
        Assert.DoesNotContain("onload", svg);

        // Belge hala gecerli XML olmali.
        XDocument.Parse(svg);
    }

    /// <summary>
    /// Ikinci savunma katmani: ayar dogrulamasi atlanip dogrudan cizim
    /// seceneklerine zararli deger konsa bile SVG bozulmamali.
    /// </summary>
    [Fact]
    public void Cizim_katmani_da_gecersiz_rengi_yok_sayar()
    {
        var options = new BadgeRenderOptions(BadgeTheme.Dark, AppLanguage.Turkish)
        {
            Background = "#000\"/><script>alert(1)</script>",
        };

        var svg = _service.GenerateStreakBadge(Data(), options);

        Assert.DoesNotContain("<script>", svg);
        XDocument.Parse(svg);
    }

    // ------------------------------------------------------------------
    // Ozel renklerin uygulanmasi
    // ------------------------------------------------------------------

    [Fact]
    public void Ozel_renkler_rozete_uygulanir()
    {
        var settings = new BadgeSettings
        {
            FlameFrom = "#00ff00",
            FlameTo = "#0000ff",
            Background = "#123456",
            Border = "#abcdef",
        };

        var svg = _service.GenerateStreakBadge(
            Data(), settings.ToRenderOptions(AppLanguage.Turkish));

        Assert.Contains("#00ff00", svg);
        Assert.Contains("#0000ff", svg);
        Assert.Contains("#123456", svg);
        Assert.Contains("#abcdef", svg);

        // Temanin varsayilan arka plani artik kullanilmamali.
        Assert.DoesNotContain("#0d1117", svg);
    }

    [Fact]
    public void Renk_verilmezse_temanin_rengi_kullanilir()
    {
        var svg = _service.GenerateStreakBadge(
            Data(), BadgeSettings.Default.ToRenderOptions(AppLanguage.Turkish));

        Assert.Contains("#0d1117", svg);
    }

    [Fact]
    public void Ozel_renk_ETag_i_degistirir()
    {
        var plain = _service.ComputeETag(
            Data(), BadgeSettings.Default.ToRenderOptions(AppLanguage.Turkish));

        var colored = _service.ComputeETag(
            Data(),
            new BadgeSettings { FlameFrom = "#00ff00" }.ToRenderOptions(AppLanguage.Turkish));

        Assert.NotEqual(plain, colored);
    }

    // ------------------------------------------------------------------
    // Saklama ve imza
    // ------------------------------------------------------------------

    [Fact]
    public void Ayarlar_JSON_e_yazilip_geri_okunabilir()
    {
        var original = new BadgeSettings
        {
            Theme = BadgeTheme.Dracula,
            Variant = BadgeVariant.Max,
            Animated = false,
            FlameFrom = "#00ff00",
        };

        var restored = BadgeSettings.FromJson(original.ToJson());

        Assert.Equal(original, restored);
    }

    /// <summary>Bozuk veya eski bir kayit uygulamayi dusurmemeli.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bu json degil")]
    [InlineData("{\"theme\":")]
    public void Bozuk_JSON_varsayilana_duser(string? json)
    {
        Assert.Equal(BadgeSettings.Default, BadgeSettings.FromJson(json));
    }

    [Fact]
    public void Kayitli_gecersiz_renk_okunurken_ayiklanir()
    {
        // Veritabanina bir sekilde gecersiz deger girdiyse cizime ulasmamali.
        var restored = BadgeSettings.FromJson("""{"background":"javascript:alert(1)"}""");

        Assert.Null(restored.Background);
    }

    [Fact]
    public void Ayni_ayar_ayni_imzayi_uretir()
    {
        var a = new BadgeSettings { Theme = BadgeTheme.Nord, FlameFrom = "#123456" };
        var b = new BadgeSettings { Theme = BadgeTheme.Nord, FlameFrom = "#123456" };

        Assert.Equal(a.ComputeSignature(), b.ComputeSignature());
    }

    /// <summary>
    /// Imzanin tek gorevi onbellek tazelemektir: ayar degisince degismezse
    /// kullanici profilindeki rozetin guncellendigini uzun sure goremez.
    /// </summary>
    [Fact]
    public void Ayar_degisince_imza_da_degisir()
    {
        var before = new BadgeSettings { Theme = BadgeTheme.Nord }.ComputeSignature();
        var afterTheme = new BadgeSettings { Theme = BadgeTheme.Dracula }.ComputeSignature();
        var afterColor = new BadgeSettings { Theme = BadgeTheme.Nord, FlameFrom = "#123456" }.ComputeSignature();
        var afterVariant = new BadgeSettings { Theme = BadgeTheme.Nord, Variant = BadgeVariant.Max }.ComputeSignature();

        Assert.NotEqual(before, afterTheme);
        Assert.NotEqual(before, afterColor);
        Assert.NotEqual(before, afterVariant);
    }

    [Fact]
    public void Imza_adreste_kullanilabilecek_kadar_kisadir()
    {
        var signature = BadgeSettings.Default.ComputeSignature();

        Assert.Equal(8, signature.Length);
        Assert.Matches("^[0-9a-f]+$", signature);
    }

    // ------------------------------------------------------------------
    // Enum -> metin donusumu
    // ------------------------------------------------------------------

    /// <summary>
    /// Enum'lar API'de METIN olarak dondurulmeli.
    /// <para>
    /// Bu test gercek bir hatadan dogdu: ayarlar arayuze sayi olarak donuyordu
    /// (<c>{"theme":0,"variant":0}</c>) ve ozellestirme sayfasi
    /// "Cannot read properties of undefined" ile cokuyordu. Ayrica sayi dondurmek,
    /// enum siralamasi degistiginde kayitli tercihlerin sessizce baska bir temaya
    /// kaymasi demektir.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(BadgeTheme.Dark, "dark")]
    [InlineData(BadgeTheme.Light, "light")]
    [InlineData(BadgeTheme.Dracula, "dracula")]
    [InlineData(BadgeTheme.TokyoNight, "tokyo-night")]
    [InlineData(BadgeTheme.Nord, "nord")]
    [InlineData(BadgeTheme.Catppuccin, "catppuccin")]
    public void Tema_metin_karsiligina_cevrilir(BadgeTheme theme, string expected)
    {
        Assert.Equal(expected, theme.ToCode());
    }

    [Theory]
    [InlineData(BadgeVariant.Full, "full")]
    [InlineData(BadgeVariant.Compact, "compact")]
    [InlineData(BadgeVariant.Max, "max")]
    public void Varyant_metin_karsiligina_cevrilir(BadgeVariant variant, string expected)
    {
        Assert.Equal(expected, variant.ToCode());
    }

    /// <summary>
    /// Metne cevrilen deger geri okunabilmeli; aksi halde kaydedilen tercih
    /// bir sonraki acilista kaybolur.
    /// </summary>
    [Fact]
    public void Tema_metni_geri_ayni_temaya_cozulur()
    {
        foreach (var theme in Enum.GetValues<BadgeTheme>())
            Assert.Equal(theme, BadgeRenderOptions.ParseTheme(theme.ToCode()));
    }

    [Fact]
    public void Varyant_metni_geri_ayni_varyanta_cozulur()
    {
        foreach (var variant in Enum.GetValues<BadgeVariant>())
            Assert.Equal(variant, BadgeRenderOptions.ParseVariant(variant.ToCode()));
    }

    // ------------------------------------------------------------------
    // Max varyant
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(AppLanguage.Turkish)]
    [InlineData(AppLanguage.English)]
    public void Max_rozet_gecerli_xml_uretir(AppLanguage language)
    {
        var svg = _service.GenerateStreakBadge(
            Data(), new BadgeRenderOptions(Variant: BadgeVariant.Max, Language: language));

        XDocument.Parse(svg);
    }

    [Fact]
    public void Max_rozet_README_genisligini_kaplar()
    {
        var svg = _service.GenerateStreakBadge(
            Data(), new BadgeRenderOptions(Variant: BadgeVariant.Max));

        var document = XDocument.Parse(svg);

        Assert.Equal("850", document.Root!.Attribute("width")!.Value);
        Assert.Equal("200", document.Root!.Attribute("height")!.Value);
    }

    [Fact]
    public void Max_rozet_tum_bilgileri_gosterir()
    {
        var svg = _service.GenerateStreakBadge(
            Data(current: 30), new BadgeRenderOptions(Variant: BadgeVariant.Max));

        Assert.Contains(">30<", svg);      // guncel seri
        Assert.Contains("REKOR", svg);     // rekor bolumu
        Assert.Contains("SON COMMIT", svg);
        Assert.Contains("ATES", svg);      // rutbe
        Assert.Contains("@Berkowhiskey", svg);
    }

    [Theory]
    [InlineData("max", BadgeVariant.Max)]
    [InlineData("MAX", BadgeVariant.Max)]
    [InlineData("compact", BadgeVariant.Compact)]
    [InlineData("full", BadgeVariant.Full)]
    [InlineData(null, BadgeVariant.Full)]
    public void Varyant_adi_adresten_cozulur(string? raw, BadgeVariant expected)
    {
        Assert.Equal(expected, BadgeRenderOptions.ParseVariant(raw));
    }

    [Fact]
    public void Her_varyant_farkli_boyutta_uretilir()
    {
        var sizes = Enum.GetValues<BadgeVariant>()
            .Select(v => _service.GenerateStreakBadge(Data(), new BadgeRenderOptions(Variant: v)))
            .Select(svg => XDocument.Parse(svg).Root!.Attribute("width")!.Value)
            .ToList();

        Assert.Equal(sizes.Count, sizes.Distinct().Count());
    }
}
