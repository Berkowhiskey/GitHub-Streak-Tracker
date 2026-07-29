namespace StreakTracker.API.Exceptions;

/// <summary>
/// GitHub API cagrilarinda olusan ve cagiran katmanin anlamli sekilde ele alabilecegi hata.
/// Octokit / HTTP detaylarini disariya sizdirmadan sarmalar.
/// </summary>
public class GitHubServiceException : Exception
{
    public GitHubServiceException(string message) : base(message)
    {
    }

    public GitHubServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>GitHub rate-limit sinirina takildiysak true olur; job'lar bu durumda yeniden deneme planlar.</summary>
    public bool IsRateLimited { get; init; }

    /// <summary>Rate-limit penceresinin sifirlanacagi an (UTC).</summary>
    public DateTimeOffset? RateLimitResetsAt { get; init; }
}
