using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StreakTracker.API.Data;
using StreakTracker.API.Entities;
using StreakTracker.API.Exceptions;
using StreakTracker.API.Models.Auth;
using StreakTracker.API.Options;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="IAuthService" />
public class AuthService : IAuthService
{
    private const string AuthorizeEndpoint = "https://github.com/login/oauth/authorize";
    private const string TokenEndpoint = "https://github.com/login/oauth/access_token";

    /// <summary>
    /// Istenen izinler:
    /// - repo      : gizli bildirim reposunu olusturmak, Issue'ya yorum atmak ve
    ///               private repo commit'lerinin streak'e sayilmasi icin zorunludur.
    /// - read:user : profil bilgisi ve katki takvimi icin.
    /// - user:email: bildirim fallback kanali (e-posta) icin.
    /// </summary>
    private const string RequestedScopes = "repo read:user user:email";

    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGitHubService _gitHubService;
    private readonly GitHubOptions _options;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IJwtTokenService jwtTokenService,
        IGitHubService gitHubService,
        IOptions<GitHubOptions> options,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _jwtTokenService = jwtTokenService;
        _gitHubService = gitHubService;
        _options = options.Value;
        _logger = logger;
    }

    public string BuildAuthorizationUrl(string state)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException(
                "GitHub:ClientId tanimli degil. appsettings.Development.json dosyasini doldurun.");
        }

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.CallbackUrl,
            ["scope"] = RequestedScopes,
            ["state"] = state
        };

        var queryString = string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));

        return $"{AuthorizeEndpoint}?{queryString}";
    }

    public async Task<AuthResultDto> HandleCallbackAsync(string code, CancellationToken cancellationToken = default)
    {
        var accessToken = await ExchangeCodeForTokenAsync(code, cancellationToken);
        var gitHubUser = await _gitHubService.GetAuthenticatedUserAsync(accessToken, cancellationToken);

        var user = await UpsertUserAsync(gitHubUser, accessToken, cancellationToken);

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        return new AuthResultDto(
            Token: token,
            ExpiresAt: expiresAt,
            User: MapToDto(user),
            // Onay verilmemisse veya bildirim altyapisi kurulmamissa onboarding gerekir.
            RequiresOnboarding: !user.HasAcceptedTerms || user.NotificationIssueNumber is null);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : MapToDto(user);
    }

    // -----------------------------------------------------------------------
    // Yardimcilar
    // -----------------------------------------------------------------------

    /// <summary>
    /// GitHub'in tek kullanimlik "code" degerini kalici access token ile degistirir.
    /// </summary>
    private async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = _options.CallbackUrl
            })
        };

        // GitHub varsayilan olarak form-encoded doner; JSON istedigimizi acikca belirtiyoruz.
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd(_options.ProductHeaderName);

        var client = _httpClientFactory.CreateClient(nameof(AuthService));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GitHub token endpoint'ine ulasilamadi.");
            throw new GitHubServiceException("GitHub kimlik dogrulama servisine ulasilamadi.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "GitHub token degisimi basarisiz. Durum: {StatusCode}", (int)response.StatusCode);

            throw new GitHubServiceException(
                $"GitHub token degisimi {(int)response.StatusCode} ile basarisiz oldu.");
        }

        GitHubTokenResponse? tokenResponse;
        try
        {
            tokenResponse = JsonSerializer.Deserialize<GitHubTokenResponse>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "GitHub token yaniti cozumlenemedi.");
            throw new GitHubServiceException("GitHub token yaniti cozumlenemedi.", ex);
        }

        if (tokenResponse is null || !string.IsNullOrWhiteSpace(tokenResponse.Error))
        {
            // Hata detayini logliyoruz ama disariya sizdirmiyoruz; token/secret bilgisi icerebilir.
            _logger.LogError(
                "GitHub token degisimi hata dondu: {Error} - {Description}",
                tokenResponse?.Error, tokenResponse?.ErrorDescription);

            throw new GitHubServiceException(
                "GitHub yetkilendirmesi tamamlanamadi. Lutfen tekrar deneyin.");
        }

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new GitHubServiceException("GitHub gecerli bir access token dondurmedi.");
        }

        _logger.LogInformation("GitHub access token alindi. Verilen scope'lar: {Scope}", tokenResponse.Scope);

        return tokenResponse.AccessToken;
    }

    /// <summary>
    /// Kullaniciyi GitHubId uzerinden bulur; yoksa olusturur, varsa profil bilgilerini
    /// ve access token'i tazeler. GitHub kullanici adi degisebildigi icin eslesme
    /// her zaman degismez olan GitHubId ile yapilir.
    /// </summary>
    private async Task<User> UpsertUserAsync(
        Models.GitHub.GitHubUserInfo gitHubUser,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.GitHubId == gitHubUser.GitHubId, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                GitHubId = gitHubUser.GitHubId,
                GitHubUsername = gitHubUser.Login,
                Email = gitHubUser.Email,
                AvatarUrl = gitHubUser.AvatarUrl,
                AccessToken = accessToken,
                IsActive = true,
                // KVKK onayi ayri bir adimda alinir; onay olmadan hesabinda hicbir sey olusturmayiz.
                HasAcceptedTerms = false
            };

            _dbContext.Users.Add(user);

            _logger.LogInformation("Yeni kullanici kaydedildi: {Login} (GitHubId: {Id})",
                gitHubUser.Login, gitHubUser.GitHubId);
        }
        else
        {
            user.GitHubUsername = gitHubUser.Login;
            user.Email = gitHubUser.Email ?? user.Email;
            user.AvatarUrl = gitHubUser.AvatarUrl ?? user.AvatarUrl;
            user.AccessToken = accessToken;

            _logger.LogInformation("Mevcut kullanici giris yapti: {Login}", gitHubUser.Login);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    private static CurrentUserDto MapToDto(User user) => new(
        user.Id,
        user.GitHubUsername,
        user.Email,
        user.AvatarUrl,
        user.HasAcceptedTerms,
        user.IsActive,
        user.PreferredNotificationHourUtc,
        user.PrivateNotificationRepoName,
        user.NotificationIssueNumber);
}
