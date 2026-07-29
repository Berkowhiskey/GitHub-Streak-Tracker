using System.Text.Json.Serialization;

namespace StreakTracker.API.Models.Auth;

/// <summary>
/// GitHub'in "code -> access_token" degisimi sonucunda dondugu yanit.
/// Hata durumunda access_token yerine error alanlari dolar.
/// </summary>
public sealed class GitHubTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
