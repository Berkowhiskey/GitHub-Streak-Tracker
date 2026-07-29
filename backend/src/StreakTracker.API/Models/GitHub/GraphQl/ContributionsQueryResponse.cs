using System.Text.Json.Serialization;

namespace StreakTracker.API.Models.GitHub.GraphQl;

// GitHub GraphQL "contributionsCollection" sorgusunun yanit sozlesmesi.
// Sadece ihtiyac duydugumuz alanlar modellenmistir.

public sealed class GraphQlResponse
{
    [JsonPropertyName("data")]
    public GraphQlData? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQlError>? Errors { get; set; }
}

public sealed class GraphQlError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class GraphQlData
{
    [JsonPropertyName("user")]
    public GraphQlUser? User { get; set; }
}

public sealed class GraphQlUser
{
    [JsonPropertyName("contributionsCollection")]
    public GraphQlContributionsCollection? ContributionsCollection { get; set; }
}

public sealed class GraphQlContributionsCollection
{
    [JsonPropertyName("contributionCalendar")]
    public GraphQlContributionCalendar? ContributionCalendar { get; set; }
}

public sealed class GraphQlContributionCalendar
{
    [JsonPropertyName("totalContributions")]
    public int TotalContributions { get; set; }

    [JsonPropertyName("weeks")]
    public List<GraphQlWeek> Weeks { get; set; } = [];
}

public sealed class GraphQlWeek
{
    [JsonPropertyName("contributionDays")]
    public List<GraphQlContributionDay> ContributionDays { get; set; } = [];
}

public sealed class GraphQlContributionDay
{
    /// <summary>ISO tarih (orn. "2026-07-28").</summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("contributionCount")]
    public int ContributionCount { get; set; }
}
