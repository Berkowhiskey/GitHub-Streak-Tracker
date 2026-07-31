using Microsoft.EntityFrameworkCore;
using StreakTracker.API.Data;
using StreakTracker.API.Models.Auth;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="IOnboardingService" />
public class OnboardingService : IOnboardingService
{
    private readonly AppDbContext _dbContext;
    private readonly IGitHubService _gitHubService;
    private readonly IStreakService _streakService;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        AppDbContext dbContext,
        IGitHubService gitHubService,
        IStreakService streakService,
        ILogger<OnboardingService> logger)
    {
        _dbContext = dbContext;
        _gitHubService = gitHubService;
        _streakService = streakService;
        _logger = logger;
    }

    public async Task<OnboardingResultDto> CompleteOnboardingAsync(
        Guid userId,
        OnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException($"Kullanici bulunamadi: {userId}");
        }

        // Onay olmadan kullanicinin GitHub hesabinda hicbir sey olusturmayiz.
        // Bu, KVKK/aydinlatma yukumlulugunun teknik karsiligidir.
        if (!request.AcceptTerms)
        {
            throw new InvalidOperationException(
                "Bildirim altyapisinin kurulabilmesi icin onay vermeniz gerekir.");
        }

        if (request.PreferredNotificationHour is { } hour)
        {
            if (hour is < 0 or > 23)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request), "Bildirim saati 0-23 araliginda olmalidir.");
            }

            user.PreferredNotificationHour = hour;
        }

        // Saat dilimi onboarding'de tarayicidan otomatik alginir; taninmayan bir
        // deger gelirse sessizce UTC'de birakiriz (bildirim akisi bloklanmamali).
        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            var resolved = UserClock.Resolve(request.TimeZoneId);
            user.TimeZoneId = resolved.Id == TimeZoneInfo.Utc.Id ? UserClock.FallbackTimeZoneId : request.TimeZoneId;
        }

        if (!user.HasAcceptedTerms)
        {
            user.HasAcceptedTerms = true;
            user.TermsAcceptedAt = DateTime.UtcNow;
        }

        // Gizli repo + sabit bildirim Issue'sunu kur (idempotent).
        var setup = await _gitHubService.SetUpNotificationInfrastructureAsync(
            user.AccessToken, user.GitHubUsername, cancellationToken);

        user.PrivateNotificationRepoName = setup.RepositoryName;
        user.NotificationIssueNumber = setup.IssueNumber;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Onboarding tamamlandi. Kullanici: {Username}, Repo: {Repo}, Issue: #{Issue}",
            user.GitHubUsername, setup.RepositoryName, setup.IssueNumber);

        // Kullanici panelde hemen gercek verisini gorsun diye ilk streak hesaplamasini burada yapiyoruz.
        var streak = await _streakService.UpdateUserStreakAsync(user.Id, cancellationToken);

        return new OnboardingResultDto(
            RepositoryName: setup.RepositoryName,
            IssueNumber: setup.IssueNumber,
            WasAlreadySetUp: setup.WasAlreadySetUp,
            CurrentStreak: streak.CurrentStreak,
            LongestStreak: streak.LongestStreak,
            HasCommittedToday: streak.HasCommittedToday,
            LastCommitDate: streak.LastCommitDate);
    }
}
