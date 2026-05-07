using System.Net;
using System.Net.Http.Json;

namespace SlackBridge.Web.Services;

public interface ISlackService
{
    Task<SlackSendResult> SendAsync(string webhookUrl, string message, CancellationToken cancellationToken);
}

public sealed record SlackSendResult(HttpStatusCode StatusCode, string ResponseBody);

public sealed class SlackDeliveryException(HttpStatusCode statusCode, string responseBody)
    : Exception($"Slack webhook returned {(int)statusCode}: {responseBody}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}

public sealed class SlackService(HttpClient httpClient, ILogger<SlackService> logger) : ISlackService
{
    public async Task<SlackSendResult> SendAsync(string webhookUrl, string message, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var response = await httpClient.PostAsJsonAsync(webhookUrl, new { text = message }, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new SlackSendResult(response.StatusCode, body);
            }

            if (!IsTransient(response.StatusCode) || attempt == 3)
            {
                throw new SlackDeliveryException(response.StatusCode, body);
            }

            logger.LogWarning("Slack webhook attempt {Attempt} failed with {StatusCode}. Retrying.", attempt, response.StatusCode);
            await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
        }

        throw new InvalidOperationException("Slack webhook retry loop exited unexpectedly.");
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError;
}
