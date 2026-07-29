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
}
