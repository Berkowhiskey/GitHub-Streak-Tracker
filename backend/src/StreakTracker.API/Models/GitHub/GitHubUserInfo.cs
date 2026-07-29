namespace StreakTracker.API.Models.GitHub;

/// <summary>
/// OAuth ile giris yapan GitHub kullanicisinin temel profil bilgileri.
/// </summary>
/// <param name="GitHubId">GitHub'in degismez numeric kullanici kimligi.</param>
/// <param name="Login">GitHub kullanici adi.</param>
/// <param name="Email">Birincil e-posta adresi (gizliyse null olabilir).</param>
/// <param name="AvatarUrl">Profil fotografi adresi.</param>
public record GitHubUserInfo(long GitHubId, string Login, string? Email, string? AvatarUrl);
