using System.Text.Json.Serialization;

namespace SlackBridge.Web.Contracts;

public sealed record SlackCommandEnvelope(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("teamId")] string? TeamId,
    [property: JsonPropertyName("teamDomain")] string? TeamDomain,
    [property: JsonPropertyName("channelId")] string? ChannelId,
    [property: JsonPropertyName("channelName")] string? ChannelName,
    [property: JsonPropertyName("userId")] string? UserId,
    [property: JsonPropertyName("userName")] string? UserName,
    [property: JsonPropertyName("command")] string? Command,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("triggerId")] string? TriggerId,
    [property: JsonPropertyName("responseUrl")] string? ResponseUrl,
    [property: JsonPropertyName("apiAppId")] string? ApiAppId,
    [property: JsonPropertyName("raw")] IReadOnlyDictionary<string, string?> Raw);
