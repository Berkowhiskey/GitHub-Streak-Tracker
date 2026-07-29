using Microsoft.AspNetCore.Http;

namespace StreakTracker.API.Options;

/// <summary>
/// appsettings.json icindeki "App" bolumunun karsiligi.
/// </summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Servisin disaridan erisilebilen adresi. Rozet baglantilari bu adres uzerinden uretilir;
    /// bu yuzden canliya cikarken gercek alan adiyla degistirilmelidir.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5157";

    /// <summary>
    /// Next.js arayuzunun adresi. GitHub girisi tamamlandiktan sonra
    /// kullanici bu adrese geri yonlendirilir.
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Access token sifreleme anahtarlarinin saklandigi klasor.
    /// <para>
    /// Container ortaminda kalici bir volume'e isaret etmelidir. Anahtarlar kaybolursa
    /// veritabanindaki tum access token'lar cozulemez hale gelir ve kullanicilar
    /// yeniden giris yapmak zorunda kalir.
    /// </para>
    /// Bos birakilirsa uygulama kokunde ".dataprotection-keys" klasoru kullanilir.
    /// </summary>
    public string DataProtectionKeysPath { get; set; } = string.Empty;

    /// <summary>
    /// Kimlik dogrulama cerezinin SameSite politikasi: <c>Lax</c> veya <c>None</c>.
    /// <para>
    /// Frontend ve API ayni site altindaysa (orn. <c>site.com</c> + <c>api.site.com</c>)
    /// <c>Lax</c> kullanilmalidir - daha guvenlidir ve Safari'de sorun cikarmaz.
    /// Farkli alan adlarindaysa (orn. Vercel + kendi sunucun) tarayici cerezi
    /// gondermez; bu durumda <c>None</c> gerekir ve HTTPS zorunlu hale gelir.
    /// </para>
    /// </summary>
    public string CookieSameSite { get; set; } = "Lax";

    /// <summary>
    /// Uygulama acilisinda bekleyen migration'lari uygular.
    /// Tek instance calistigi surece guvenlidir; birden fazla instance'a
    /// gecilirse migration ayri bir adima tasinmalidir.
    /// </summary>
    public bool RunMigrationsOnStartup { get; set; }

    /// <summary>
    /// <see cref="CookieSameSite"/> degerini dogrulayarak cozer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Deger taninmiyorsa firlatilir.</exception>
    public SameSiteMode ResolveCookieSameSite() => CookieSameSite?.Trim().ToLowerInvariant() switch
    {
        "lax" or "" or null => SameSiteMode.Lax,
        "none" => SameSiteMode.None,
        "strict" => SameSiteMode.Strict,
        _ => throw new InvalidOperationException(
            $"App:CookieSameSite gecersiz: '{CookieSameSite}'. Gecerli degerler: Lax, None, Strict.")
    };
}
