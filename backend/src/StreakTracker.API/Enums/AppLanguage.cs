namespace StreakTracker.API.Enums;

/// <summary>
/// Desteklenen arayuz/bildirim dilleri.
/// Bildirim metinleri, rozet etiketleri ve arayuz bu secime gore uretilir.
/// </summary>
public enum AppLanguage
{
    Turkish = 0,
    English = 1
}

public static class AppLanguageExtensions
{
    /// <summary>Dil kodu ("tr" / "en"). Veritabaninda ve API'de bu kod kullanilir.</summary>
    public static string ToCode(this AppLanguage language) =>
        language == AppLanguage.English ? "en" : "tr";

    /// <summary>
    /// Dil kodunu cozer. Taninmayan veya bos deger Turkce'ye duser
    /// (uygulamanin varsayilan dili).
    /// </summary>
    public static AppLanguage ParseLanguage(string? code) =>
        code?.Trim().ToLowerInvariant() switch
        {
            "en" or "en-us" or "en-gb" => AppLanguage.English,
            _ => AppLanguage.Turkish
        };

    /// <summary>Verilen kodun desteklenen bir dile karsilik gelip gelmedigi.</summary>
    public static bool IsSupported(string? code) =>
        code?.Trim().ToLowerInvariant() is "tr" or "en";
}
