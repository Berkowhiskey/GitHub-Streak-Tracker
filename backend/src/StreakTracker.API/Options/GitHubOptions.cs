namespace StreakTracker.API.Options;

/// <summary>
/// appsettings.json icindeki "GitHub" bolumunun karsiligi.
/// </summary>
public class GitHubOptions
{
    public const string SectionName = "GitHub";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>Octokit'in GitHub API'sine gonderdigi User-Agent adi.</summary>
    public string ProductHeaderName { get; set; } = "StreakTracker";

    /// <summary>Kullanicinin hesabinda olusturulacak gizli bildirim reposunun adi.</summary>
    public string NotificationRepoName { get; set; } = ".streak-tracker-notifications";
}
