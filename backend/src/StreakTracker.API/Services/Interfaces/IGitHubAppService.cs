namespace StreakTracker.API.Services.Interfaces;

/// <summary>
/// GitHub App kimligi ile calisan islemler.
///
/// <para>
/// <b>Neden gerekli:</b> GitHub, kullanicinin kendi yaptigi eylemler icin ona bildirim
/// gondermez. Bildirim yorumu kullanicinin kendi token'iyla atilirsa Issue'da gorunur
/// ama telefona push dusmez. Yorumu App (bot) kimligi attiginda ise farkli bir kullanici
/// soz konusu oldugu icin GitHub bildirimi uretir.
/// </para>
/// </summary>
public interface IGitHubAppService
{
    /// <summary>App yapilandirilmis mi (AppId + PrivateKey mevcut mu)?</summary>
    bool IsConfigured { get; }

    /// <summary>Kullanicinin App'i kuracagi adres.</summary>
    string InstallationUrl { get; }

    /// <summary>
    /// Kullanicinin App'i hesabina kurup kurmadigini kontrol eder.
    /// </summary>
    /// <returns>Kurulum varsa installation kimligi, yoksa null.</returns>
    Task<long?> GetInstallationIdAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bildirim Issue'suna <b>bot kimligiyle</b> yorum ekler.
    /// Kullanici @mention edildigi icin GitHub Mobile push bildirimi uretir.
    /// </summary>
    Task SendNotificationCommentAsync(
        long installationId,
        string username,
        string repositoryName,
        int issueNumber,
        string message,
        CancellationToken cancellationToken = default);
}
