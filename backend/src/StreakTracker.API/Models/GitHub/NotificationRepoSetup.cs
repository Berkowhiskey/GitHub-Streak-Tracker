namespace StreakTracker.API.Models.GitHub;

/// <summary>
/// Onboarding sirasinda kurulan bildirim altyapisinin sonucu:
/// gizli repo adi ve bildirim yorumlarinin dusulecegi sabit Issue numarasi.
/// </summary>
/// <param name="RepositoryName">Olusturulan (veya zaten var olan) gizli reponun adi.</param>
/// <param name="IssueNumber">Bildirim Issue'sunun numarasi.</param>
/// <param name="WasAlreadySetUp">Kurulum daha onceden yapilmissa true (idempotent calisma).</param>
public record NotificationRepoSetup(string RepositoryName, int IssueNumber, bool WasAlreadySetUp);
