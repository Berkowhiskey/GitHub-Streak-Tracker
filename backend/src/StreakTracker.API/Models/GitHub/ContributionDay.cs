namespace StreakTracker.API.Models.GitHub;

/// <summary>
/// GitHub contribution takviminde tek bir gun.
/// </summary>
/// <param name="Date">Gunun UTC tarihi.</param>
/// <param name="ContributionCount">O gun yapilan katki (commit, PR, issue vb.) sayisi.</param>
public record ContributionDay(DateOnly Date, int ContributionCount)
{
    public bool HasContribution => ContributionCount > 0;
}
