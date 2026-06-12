# Slack Slash Commands

Slack Bridge exposes a minimal inbound gateway for slash commands:

```http
POST /api/slack/commands
Content-Type: application/x-www-form-urlencoded
```

Configure routes in **Admin > Slack commands**. A route belongs to a Slack bot/project and stores the Slack signing secret plus downstream shared secret encrypted with ASP.NET Core Data Protection.

## Slack App Setup

1. In Slack, create or open an app.
2. Enable **Slash Commands**.
3. Create the command, for example `/shoppingtajm`.
4. Set **Request URL** to `https://your-slack-bridge.example.com/api/slack/commands`.
5. Copy the app **Signing Secret** into the matching SlackBridge route.

## Downstream Request

Slack Bridge verifies Slack's signature and timestamp before forwarding JSON to the configured downstream URL. If a downstream auth secret is configured, Slack Bridge sends it using the configured header name, defaulting to `x-slackbridge-secret`.

```json
{
  "type": "slash_command",
  "teamId": "T123",
  "teamDomain": "example",
  "channelId": "C123",
  "channelName": "general",
  "userId": "U123",
  "userName": "deprecated_from_slack_if_present_but_not_required",
  "command": "/shoppingtajm",
  "text": "list",
  "triggerId": "123.456",
  "responseUrl": "https://hooks.slack.com/commands/...",
  "apiAppId": "A123",
  "raw": {
    "command": "/shoppingtajm",
    "text": "list"
  }
}
```

The `raw` object is parsed form data with Slack's legacy `token` field removed.

## Downstream Response

Downstream apps can return:

- `200` with a Slack-compatible JSON object, which Slack Bridge relays.
- `204`, which Slack Bridge turns into an empty `200`.
- `4xx`, `5xx`, invalid JSON, timeout, or transport failure, which Slack Bridge turns into a safe ephemeral fallback.

Example response:

```json
{
  "response_type": "ephemeral",
  "text": "Aktiv lista: Familjen\n- mjolk\n- kaffe"
}
```

Fallback response:

```json
{
  "response_type": "ephemeral",
  "text": "Kommandot togs emot men kunde inte behandlas just nu."
}
```

SlackBridge does not implement app-specific command parsing or shopping-list logic.
