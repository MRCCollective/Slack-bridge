using System.Net;
using System.Text;
using System.Text.Json;
using SlackBridge.Web.Contracts;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface IDownstreamSlackCommandClient
{
    Task<SlackCommandForwardResult> ForwardAsync(
        SlackCommandRoute route,
        string? downstreamSecret,
        SlackCommandEnvelope envelope,
        CancellationToken cancellationToken);
}

public sealed class SlackCommandForwarder(
    HttpClient httpClient,
    ILogger<SlackCommandForwarder> logger) : IDownstreamSlackCommandClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DownstreamTimeout = TimeSpan.FromSeconds(2.5);

    public async Task<SlackCommandForwardResult> ForwardAsync(
        SlackCommandRoute route,
        string? downstreamSecret,
        SlackCommandEnvelope envelope,
        CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(DownstreamTimeout);
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var payload = JsonSerializer.Serialize(envelope, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, route.DownstreamUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(route.DownstreamAuthHeaderName) &&
            !string.IsNullOrWhiteSpace(downstreamSecret))
        {
            request.Headers.TryAddWithoutValidation(route.DownstreamAuthHeaderName, downstreamSecret);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, linkedToken.Token);
            var statusCode = (int)response.StatusCode;

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return SlackCommandForwardResult.EmptyAck(statusCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                return SlackCommandForwardResult.Fallback(statusCode, $"Downstream returned {(int)response.StatusCode}.");
            }

            var responseBody = await response.Content.ReadAsStringAsync(linkedToken.Token);
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return SlackCommandForwardResult.EmptyAck(statusCode);
            }

            if (!IsJsonObject(responseBody))
            {
                return SlackCommandForwardResult.Fallback(statusCode, "Downstream returned non-JSON content.");
            }

            return SlackCommandForwardResult.SlackJson(statusCode, responseBody);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            logger.LogWarning(
                "Downstream slash command route {RouteId} timed out for {Command}.",
                route.Id,
                route.SlackCommand);

            return SlackCommandForwardResult.Fallback(null, "Downstream timed out.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Downstream slash command route {RouteId} failed for {Command}.",
                route.Id,
                route.SlackCommand);

            return SlackCommandForwardResult.Fallback(null, exception.Message);
        }
    }

    private static bool IsJsonObject(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record SlackCommandForwardResult(
    int? DownstreamStatusCode,
    bool UseFallback,
    bool HasSlackJsonResponse,
    string? SlackJsonResponse,
    string ResultMessage)
{
    public static SlackCommandForwardResult SlackJson(int downstreamStatusCode, string responseJson) =>
        new(downstreamStatusCode, false, true, responseJson, "Downstream Slack response relayed.");

    public static SlackCommandForwardResult EmptyAck(int downstreamStatusCode) =>
        new(downstreamStatusCode, false, false, null, "Downstream acknowledged without content.");

    public static SlackCommandForwardResult Fallback(int? downstreamStatusCode, string message) =>
        new(downstreamStatusCode, true, true, null, message);
}
