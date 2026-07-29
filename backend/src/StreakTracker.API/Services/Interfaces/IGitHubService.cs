using StreakTracker.API.Models.GitHub;

namespace StreakTracker.API.Services.Interfaces;

/// <summary>
/// GitHub REST (Octokit) ve GraphQL API'leri ile konusan tek nokta.
/// Uygulamanin geri kalani GitHub'a dogrudan erismez; her sey bu servis uzerinden gecer.
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// Verilen access token'a sahip kullanicinin GitHub profil bilgilerini dondurur.
    /// E-posta gizliyse "user:email" scope'u uzerinden birincil adres ayrica sorgulanir.
    /// </summary>
    Task<GitHubUserInfo> GetAuthenticatedUserAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanicinin belirtilen tarih araligindaki gunluk katki sayilarini dondurur.
    /// GraphQL contributionsCollection kullanir; token "repo" scope'una sahipse
    /// private repo katkilari da dahil edilir.
    /// </summary>
    /// <param name="from">Baslangic tarihi (dahil).</param>
    /// <param name="to">Bitis tarihi (dahil). GitHub en fazla 1 yillik aralik kabul eder.</param>
    Task<IReadOnlyList<ContributionDay>> GetContributionDaysAsync(
        string accessToken,
        string username,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanicinin icinde bulunulan UTC gununde commit/katki yapip yapmadigini dondurur.
    /// </summary>
    Task<bool> HasUserCommittedTodayAsync(
        string accessToken,
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bildirimler icin kullanilacak gizli (private) repoyu olusturur.
    /// Repo zaten varsa yeniden olusturmaz; islem idempotenttir.
    /// </summary>
    /// <returns>Reponun adi.</returns>
    Task<string> CreatePrivateNotificationRepoAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gizli repoda bildirim yorumlarinin dusulecegi sabit Issue'yu hazirlar.
    /// Uygun bir Issue zaten varsa onun numarasini dondurur.
    /// </summary>
    Task<int> EnsureNotificationIssueExistsAsync(
        string accessToken,
        string username,
        string repositoryName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gizli repo + bildirim Issue'sunu tek adimda kurar (1-Click Onboarding icin).
    /// </summary>
    Task<NotificationRepoSetup> SetUpNotificationInfrastructureAsync(
        string accessToken,
        string username,
        CancellationToken cancellationToken = default);

    // Not: Bildirim yorumlari artik bu servis uzerinden DEGIL, IGitHubAppService
    // uzerinden bot kimligiyle gonderilir. GitHub, kullanicinin kendi eylemleri icin
    // bildirim uretmedigi icin yorumun farkli bir kimlikten gelmesi zorunludur.
}
