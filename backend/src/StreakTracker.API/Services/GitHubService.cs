using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Octokit;
using StreakTracker.API.Exceptions;
using StreakTracker.API.Models.GitHub;
using StreakTracker.API.Models.GitHub.GraphQl;
using StreakTracker.API.Options;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="IGitHubService" />
public class GitHubService : IGitHubService
{
    /// <summary>Bildirim Issue'sunun sabit basligi. Var olan Issue bu baslikla bulunur.</summary>
    public const string NotificationIssueTitle = "🔥 Streak Bildirimleri";

    private const string GraphQlEndpoint = "https://api.github.com/graphql";

    /// <summary>
    /// contributionsCollection, private repo katkilarini yalnizca token "repo" scope'una
    /// sahipse dondurur; onboarding sirasinda bu scope talep edilir.
    /// </summary>
    private const string ContributionsQuery = """
        query($login: String!, $from: DateTime!, $to: DateTime!) {
          user(login: $login) {
            contributionsCollection(from: $from, to: $to) {
              contributionCalendar {
                totalContributions
                weeks {
                  contributionDays {
                    date
                    contributionCount
                  }
                }
              }
            }
          }
        }
        """;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(
        IHttpClientFactory httpClientFactory,
        IOptions<GitHubOptions> options,
        ILogger<GitHubService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Kullanici profili - REST (Octokit)
    // -----------------------------------------------------------------------

    public async Task<GitHubUserInfo> GetAuthenticatedUserAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var client = CreateOctokitClient(accessToken);

        try
        {
            var user = await client.User.Current();
            var email = user.Email;

            // Kullanici e-postasini gizlemisse profilde bos gelir; "user:email" scope'u ile
            // dogrulanmis birincil adresi ayrica sorgulariz. Bu adim basarisiz olursa
            // e-posta olmadan devam ederiz - giris akisini bloklamamalidir.
            if (string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    var emails = await client.User.Email.GetAll();
                    email = emails.FirstOrDefault(e => e is { Primary: true, Verified: true })?.Email;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kullanicinin e-posta adresi alinamadi: {Login}", user.Login);
                }
            }

            return new GitHubUserInfo(user.Id, user.Login, email, user.AvatarUrl);
        }
        catch (Exception ex)
        {
            throw WrapGitHubException(ex, "GitHub kullanici profili alinamadi.");
        }
    }

    // -----------------------------------------------------------------------
    // Katki (contribution) sorgulari - GraphQL
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<ContributionDay>> GetContributionDaysAsync(
        string accessToken,
        string username,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
            throw new ArgumentException("Baslangic tarihi bitis tarihinden sonra olamaz.", nameof(from));

        var payload = JsonSerializer.Serialize(new
        {
            query = ContributionsQuery,
            variables = new
            {
                login = username,
                // Gunun tamamini kapsayacak sekilde: 00:00:00Z -> 23:59:59Z
                from = from.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                to = to.ToDateTime(new TimeOnly(23, 59, 59)).ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd(_options.ProductHeaderName);

        var client = _httpClientFactory.CreateClient(nameof(GitHubService));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GitHub GraphQL API'sine ulasilamadi. Kullanici: {Username}", username);
            throw new GitHubServiceException("GitHub API'sine ulasilamadi.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // GitHub GraphQL rate-limit asimini 200 yerine 403/429 ile de bildirebilir.
            var rateLimited = response.StatusCode is System.Net.HttpStatusCode.Forbidden
                                                  or System.Net.HttpStatusCode.TooManyRequests;

            _logger.LogError(
                "GitHub GraphQL sorgusu basarisiz. Kullanici: {Username}, Durum: {StatusCode}, Yanit: {Body}",
                username, (int)response.StatusCode, Truncate(body, 500));

            throw new GitHubServiceException($"GitHub GraphQL sorgusu {(int)response.StatusCode} ile basarisiz oldu.")
            {
                IsRateLimited = rateLimited,
                RateLimitResetsAt = ReadRateLimitReset(response)
            };
        }

        GraphQlResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<GraphQlResponse>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "GitHub GraphQL yaniti cozumlenemedi. Kullanici: {Username}", username);
            throw new GitHubServiceException("GitHub yaniti cozumlenemedi.", ex);
        }

        if (parsed?.Errors is { Count: > 0 })
        {
            var message = string.Join(" | ", parsed.Errors.Select(e => e.Message));
            _logger.LogError("GitHub GraphQL hatasi. Kullanici: {Username}, Hata: {Error}", username, message);

            throw new GitHubServiceException($"GitHub GraphQL hatasi: {message}")
            {
                IsRateLimited = parsed.Errors.Any(e => e.Type == "RATE_LIMITED")
            };
        }

        var calendar = parsed?.Data?.User?.ContributionsCollection?.ContributionCalendar;

        if (calendar is null)
        {
            _logger.LogWarning("GitHub kullanicisi bulunamadi veya katki takvimi bos: {Username}", username);
            return [];
        }

        var days = calendar.Weeks
            .SelectMany(w => w.ContributionDays)
            .Where(d => DateOnly.TryParse(d.Date, out _))
            .Select(d => new ContributionDay(DateOnly.Parse(d.Date), d.ContributionCount))
            .OrderBy(d => d.Date)
            .ToList();

        _logger.LogDebug(
            "{Username} icin {DayCount} gunluk katki verisi alindi (toplam {Total} katki).",
            username, days.Count, calendar.TotalContributions);

        return days;
    }

    public async Task<bool> HasUserCommittedTodayAsync(
        string accessToken,
        string username,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = await GetContributionDaysAsync(accessToken, username, today, today, cancellationToken);

        return days.Any(d => d.Date == today && d.HasContribution);
    }

    // -----------------------------------------------------------------------
    // Bildirim altyapisi - REST (Octokit)
    // -----------------------------------------------------------------------

    public async Task<string> CreatePrivateNotificationRepoAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var client = CreateOctokitClient(accessToken);
        var repoName = _options.NotificationRepoName;

        var newRepo = new NewRepository(repoName)
        {
            Private = true,
            AutoInit = true,
            HasIssues = true,
            HasWiki = false,
            HasDownloads = false,
            Description = "StreakTracker bildirimleri icin otomatik olusturuldu. Bu repo gizlidir."
        };

        try
        {
            var created = await client.Repository.Create(newRepo);
            _logger.LogInformation("Gizli bildirim reposu olusturuldu: {FullName}", created.FullName);
            return created.Name;
        }
        catch (RepositoryExistsException)
        {
            // Kullanici daha once kaydolmus ya da repoyu kendisi olusturmus; islem idempotent.
            _logger.LogInformation("Bildirim reposu zaten mevcut: {RepoName}", repoName);
            return repoName;
        }
        catch (Exception ex)
        {
            throw WrapGitHubException(ex, $"Gizli bildirim reposu ({repoName}) olusturulamadi.");
        }
    }

    public async Task<int> EnsureNotificationIssueExistsAsync(
        string accessToken,
        string username,
        string repositoryName,
        CancellationToken cancellationToken = default)
    {
        var client = CreateOctokitClient(accessToken);

        try
        {
            var existing = await client.Issue.GetAllForRepository(username, repositoryName, new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open
            });

            var notificationIssue = existing.FirstOrDefault(i => i.Title == NotificationIssueTitle);

            if (notificationIssue is not null)
            {
                _logger.LogInformation(
                    "Bildirim Issue'su zaten mevcut. Kullanici: {Username}, Issue: #{Number}",
                    username, notificationIssue.Number);

                return notificationIssue.Number;
            }

            var newIssue = new NewIssue(NotificationIssueTitle)
            {
                Body = BuildNotificationIssueBody(username)
            };

            var created = await client.Issue.Create(username, repositoryName, newIssue);

            _logger.LogInformation(
                "Bildirim Issue'su olusturuldu. Kullanici: {Username}, Issue: #{Number}",
                username, created.Number);

            return created.Number;
        }
        catch (Exception ex)
        {
            throw WrapGitHubException(ex, $"Bildirim Issue'su hazirlanamadi ({username}/{repositoryName}).");
        }
    }

    public async Task<NotificationRepoSetup> SetUpNotificationInfrastructureAsync(
        string accessToken,
        string username,
        CancellationToken cancellationToken = default)
    {
        var client = CreateOctokitClient(accessToken);
        var repoName = _options.NotificationRepoName;

        var alreadySetUp = false;

        try
        {
            await client.Repository.Get(username, repoName);
            alreadySetUp = true;
            _logger.LogInformation("Bildirim altyapisi zaten kurulu: {Username}/{RepoName}", username, repoName);
        }
        catch (NotFoundException)
        {
            // Repo yok; asagida olusturulacak.
        }
        catch (Exception ex)
        {
            throw WrapGitHubException(ex, $"Bildirim reposu ({username}/{repoName}) sorgulanamadi.");
        }

        if (!alreadySetUp)
        {
            repoName = await CreatePrivateNotificationRepoAsync(accessToken, cancellationToken);
        }

        var issueNumber = await EnsureNotificationIssueExistsAsync(accessToken, username, repoName, cancellationToken);

        return new NotificationRepoSetup(repoName, issueNumber, alreadySetUp);
    }

    public async Task SendNotificationCommentAsync(
        string accessToken,
        string username,
        string repositoryName,
        int issueNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        var client = CreateOctokitClient(accessToken);

        try
        {
            await client.Issue.Comment.Create(username, repositoryName, issueNumber, message);

            _logger.LogInformation(
                "Bildirim yorumu gonderildi. Kullanici: {Username}, Issue: #{Number}",
                username, issueNumber);
        }
        catch (Exception ex)
        {
            throw WrapGitHubException(ex,
                $"Bildirim yorumu gonderilemedi ({username}/{repositoryName}#{issueNumber}).");
        }
    }

    // -----------------------------------------------------------------------
    // Yardimcilar
    // -----------------------------------------------------------------------

    private GitHubClient CreateOctokitClient(string accessToken)
    {
        // Octokit'in ProductHeaderValue'su, System.Net.Http.Headers'daki ayni isimli tiple karismasin.
        return new GitHubClient(new Octokit.ProductHeaderValue(_options.ProductHeaderName))
        {
            Credentials = new Credentials(accessToken)
        };
    }

    private static string BuildNotificationIssueBody(string username)
    {
        return $"""
            Merhaba @{username}! 👋

            Bu Issue **StreakTracker** tarafindan otomatik olarak olusturuldu.

            Streak'in tehlikeye girdiginde buraya bir yorum dusecegiz. GitHub Mobile
            uygulaman bu yorumu aninda telefonuna push bildirimi olarak iletecek.

            > ⚠️ Bildirimlerin calismaya devam etmesi icin lutfen **bu Issue'yu kapatma**
            > ve bu repoyu silme. Repo gizlidir; icerigini yalnizca sen gorebilirsin.

            Bildirimleri durdurmak istersen StreakTracker panelinden kapatabilirsin.
            """;
    }

    /// <summary>
    /// Octokit istisnalarini uygulama katmanina uygun tek bir tipe cevirir ve
    /// rate-limit durumunu isaretler; boylece job'lar yeniden deneme karari verebilir.
    /// </summary>
    private GitHubServiceException WrapGitHubException(Exception ex, string message)
    {
        switch (ex)
        {
            case RateLimitExceededException rateLimit:
                _logger.LogWarning(
                    "GitHub rate-limit asildi. Sifirlanma zamani: {Reset}. {Message}",
                    rateLimit.Reset, message);

                return new GitHubServiceException(message, rateLimit)
                {
                    IsRateLimited = true,
                    RateLimitResetsAt = rateLimit.Reset
                };

            case AuthorizationException:
                _logger.LogWarning("GitHub yetkilendirme hatasi (token gecersiz veya iptal edilmis). {Message}", message);
                return new GitHubServiceException($"{message} GitHub yetkilendirmesi gecersiz.", ex);

            case ApiException apiEx:
                _logger.LogError(apiEx, "GitHub API hatasi ({StatusCode}). {Message}", apiEx.HttpResponse?.StatusCode, message);
                return new GitHubServiceException(message, apiEx);

            default:
                _logger.LogError(ex, "Beklenmeyen GitHub hatasi. {Message}", message);
                return new GitHubServiceException(message, ex);
        }
    }

    private static DateTimeOffset? ReadRateLimitReset(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-ratelimit-reset", out var values) &&
            long.TryParse(values.FirstOrDefault(), out var epochSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        }

        return null;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
