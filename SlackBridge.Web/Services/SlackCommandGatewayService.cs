using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Contracts;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface ISlackCommandGatewayService
{
    Task<SlackCommandGatewayResult> HandleAsync(
        string requestBody,
        IFormCollection form,
        IHeaderDictionary headers,
        CancellationToken cancellationToken);
}

public sealed class SlackCommandGatewayService(
    SlackBridgeDbContext dbContext,
    ISlackRequestVerifier slackRequestVerifier,
    IDownstreamSlackCommandClient downstreamClient,
    IApiKeySecretProtector secretProtector,
    ILogger<SlackCommandGatewayService> logger) : ISlackCommandGatewayService
{
    private const int DefaultCustomerInstanceId = 1;
    private const string FallbackResponseText = "Kommandot togs emot men kunde inte behandlas just nu.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SlackCommandGatewayResult> HandleAsync(
        string requestBody,
        IFormCollection form,
        IHeaderDictionary headers,
        CancellationToken cancellationToken)
    {
        if (IsSslCheck(form))
        {
            return await HandleSslCheckAsync(requestBody, headers, cancellationToken);
        }

        var command = NormalizeCommand(Value(form, "command"));
        var teamId = Value(form, "team_id");
        if (string.IsNullOrWhiteSpace(command))
        {
            await WriteLogAsync(null, null, form, SlackCommandLogStatus.Rejected, null, "Missing Slack command.", cancellationToken);
            return SlackCommandGatewayResult.BadRequest("missing_command");
        }

        var candidates = await dbContext.SlackCommandRoutes
            .Include(route => route.Project)
            .Where(route =>
                route.IsActive &&
                route.SlackCommand == command &&
                (string.IsNullOrWhiteSpace(route.AllowedTeamId) || route.AllowedTeamId == teamId))
            .OrderBy(route => route.Id)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            await WriteLogAsync(null, null, form, SlackCommandLogStatus.Rejected, null, "No active Slack command route matched.", cancellationToken);
            logger.LogWarning(
                "Slack slash command rejected: no active route for {Command} and team {TeamId}.",
                command,
                teamId);
            return SlackCommandGatewayResult.Unauthorized("no_active_route");
        }

        var verification = FindVerifiedRoute(requestBody, headers, candidates);
        if (verification.Route is null)
        {
            var logRoute = candidates[0];
            await WriteLogAsync(
                logRoute,
                null,
                form,
                SlackCommandLogStatus.Rejected,
                null,
                verification.FailureReason ?? "Slack signature verification failed.",
                cancellationToken);

            logger.LogWarning(
                "Slack slash command rejected for route {RouteId}, command {Command}, team {TeamId}: {Reason}.",
                logRoute.Id,
                command,
                teamId,
                verification.FailureReason);

            return SlackCommandGatewayResult.Unauthorized("invalid_signature");
        }

        var route = verification.Route;
        var envelope = CreateEnvelope(form);
        var downstreamSecret = secretProtector.Unprotect(route.EncryptedDownstreamAuthSecret);
        var downstreamResult = await downstreamClient.ForwardAsync(route, downstreamSecret, envelope, cancellationToken);

        if (downstreamResult.UseFallback)
        {
            logger.LogWarning(
                "Slack slash command used fallback. RouteId={RouteId} CustomerInstanceId={CustomerInstanceId} ProjectId={ProjectId} Command={Command} TeamId={TeamId} ChannelId={ChannelId} UserId={UserId} DownstreamStatusCode={DownstreamStatusCode} Result={ResultMessage}",
                route.Id,
                route.CustomerInstanceId,
                route.ProjectId,
                envelope.Command,
                envelope.TeamId,
                envelope.ChannelId,
                envelope.UserId,
                downstreamResult.DownstreamStatusCode,
                downstreamResult.ResultMessage);

            await WriteLogAsync(
                route,
                downstreamResult.DownstreamStatusCode,
                form,
                SlackCommandLogStatus.Fallback,
                null,
                downstreamResult.ResultMessage,
                cancellationToken);

            return SlackCommandGatewayResult.Json(FallbackJson());
        }

        logger.LogInformation(
            "Slack slash command forwarded. RouteId={RouteId} CustomerInstanceId={CustomerInstanceId} ProjectId={ProjectId} Command={Command} TeamId={TeamId} ChannelId={ChannelId} UserId={UserId} DownstreamStatusCode={DownstreamStatusCode}",
            route.Id,
            route.CustomerInstanceId,
            route.ProjectId,
            envelope.Command,
            envelope.TeamId,
            envelope.ChannelId,
            envelope.UserId,
            downstreamResult.DownstreamStatusCode);

        await WriteLogAsync(
            route,
            downstreamResult.DownstreamStatusCode,
            form,
            SlackCommandLogStatus.Succeeded,
            null,
            downstreamResult.ResultMessage,
            cancellationToken);

        return downstreamResult.HasSlackJsonResponse && downstreamResult.SlackJsonResponse is not null
            ? SlackCommandGatewayResult.Json(downstreamResult.SlackJsonResponse)
            : SlackCommandGatewayResult.EmptyOk();
    }

    private async Task<SlackCommandGatewayResult> HandleSslCheckAsync(
        string requestBody,
        IHeaderDictionary headers,
        CancellationToken cancellationToken)
    {
        var routes = await dbContext.SlackCommandRoutes
            .Where(route => route.IsActive)
            .OrderBy(route => route.Id)
            .ToListAsync(cancellationToken);

        var verification = FindVerifiedRoute(requestBody, headers, routes);
        if (verification.Route is null)
        {
            logger.LogWarning("Slack ssl_check rejected: {Reason}.", verification.FailureReason);
            await WriteLogAsync(null, null, null, SlackCommandLogStatus.Rejected, null, verification.FailureReason ?? "SSL check verification failed.", cancellationToken);
            return SlackCommandGatewayResult.Unauthorized("invalid_signature");
        }

        await WriteLogAsync(verification.Route, null, null, SlackCommandLogStatus.Succeeded, null, "Slack ssl_check verified.", cancellationToken);
        logger.LogInformation(
            "Slack ssl_check verified. RouteId={RouteId} CustomerInstanceId={CustomerInstanceId} ProjectId={ProjectId}",
            verification.Route.Id,
            verification.Route.CustomerInstanceId,
            verification.Route.ProjectId);

        return SlackCommandGatewayResult.EmptyOk();
    }

    private VerifiedRoute FindVerifiedRoute(
        string requestBody,
        IHeaderDictionary headers,
        IReadOnlyList<SlackCommandRoute> candidates)
    {
        var timestamp = headers["X-Slack-Request-Timestamp"].ToString();
        var signature = headers["X-Slack-Signature"].ToString();
        string? lastFailure = null;

        foreach (var route in candidates)
        {
            var signingSecret = secretProtector.Unprotect(route.EncryptedSlackSigningSecret);
            var result = slackRequestVerifier.Verify(
                requestBody,
                timestamp,
                signature,
                signingSecret ?? string.Empty,
                DateTimeOffset.UtcNow);

            if (result.IsValid)
            {
                return new VerifiedRoute(route, null);
            }

            lastFailure = result.FailureReason;
        }

        return new VerifiedRoute(null, lastFailure);
    }

    private async Task WriteLogAsync(
        SlackCommandRoute? route,
        int? downstreamStatusCode,
        IFormCollection? form,
        SlackCommandLogStatus status,
        string? commandOverride,
        string resultMessage,
        CancellationToken cancellationToken)
    {
        dbContext.SlackCommandLogs.Add(new SlackCommandLog
        {
            CustomerInstanceId = route?.CustomerInstanceId ?? DefaultCustomerInstanceId,
            ProjectId = route?.ProjectId,
            SlackCommandRouteId = route?.Id,
            Command = commandOverride ?? NormalizeCommand(Value(form, "command")),
            TeamId = Value(form, "team_id"),
            ChannelId = Value(form, "channel_id"),
            UserId = Value(form, "user_id"),
            DownstreamStatusCode = downstreamStatusCode,
            Status = status,
            ResultMessage = resultMessage
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SlackCommandEnvelope CreateEnvelope(IFormCollection form)
    {
        return new SlackCommandEnvelope(
            "slash_command",
            Value(form, "team_id"),
            Value(form, "team_domain"),
            Value(form, "channel_id"),
            Value(form, "channel_name"),
            Value(form, "user_id"),
            Value(form, "user_name"),
            Value(form, "command"),
            Value(form, "text"),
            Value(form, "trigger_id"),
            Value(form, "response_url"),
            Value(form, "api_app_id"),
            SafeRaw(form));
    }

    private static IReadOnlyDictionary<string, string?> SafeRaw(IFormCollection form)
    {
        return form
            .Where(pair => !string.Equals(pair.Key, "token", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.ToString());
    }

    private static bool IsSslCheck(IFormCollection form) =>
        string.Equals(Value(form, "ssl_check"), "1", StringComparison.Ordinal);

    private static string? Value(IFormCollection? form, string key)
    {
        if (form is null || !form.TryGetValue(key, out var value))
        {
            return null;
        }

        var result = value.ToString();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? NormalizeCommand(string? command) =>
        string.IsNullOrWhiteSpace(command) ? null : command.Trim().ToLowerInvariant();

    private static string FallbackJson() =>
        JsonSerializer.Serialize(
            new
            {
                response_type = "ephemeral",
                text = FallbackResponseText
            },
            JsonOptions);

    private sealed record VerifiedRoute(SlackCommandRoute? Route, string? FailureReason);
}

public sealed record SlackCommandGatewayResult(
    int StatusCode,
    string? ContentType,
    string? Body)
{
    public static SlackCommandGatewayResult Json(string body) =>
        new(StatusCodes.Status200OK, "application/json", body);

    public static SlackCommandGatewayResult EmptyOk() =>
        new(StatusCodes.Status200OK, null, null);

    public static SlackCommandGatewayResult Unauthorized(string reason) =>
        new(StatusCodes.Status401Unauthorized, "text/plain", reason);

    public static SlackCommandGatewayResult BadRequest(string reason) =>
        new(StatusCodes.Status400BadRequest, "text/plain", reason);
}
