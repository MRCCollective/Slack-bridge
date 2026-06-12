# Slack Bridge

<p>
  <img src="SlackBridge.Web/wwwroot/img/slackbridge-logo.png" alt="Slack Bridge" width="420" />
</p>

Slack Bridge is an internal Slack integration gateway. It turns application events into structured Slack messages without adding Slack-specific code to every app, and it can receive Slack slash commands, verify them, and forward a normalized payload to a downstream app.

Apps send events to one central API. Slack Bridge handles authentication, template rendering, routing to the configured Slack webhook, delivery, usage tracking, and event logging. For inbound slash commands, Slack Bridge handles Slack signatures, route lookup, downstream forwarding, Slack-compatible responses, and request outcome logging.

## Why Slack Bridge?

Slack notifications across multiple systems often become inconsistent and hard to maintain:

- duplicated Slack integration logic
- different message formats in every app
- abandoned "nice-to-have" notifications
- no central audit trail when delivery fails

Slack Bridge gives teams a single configurable gateway for event-driven internal messaging.

## How It Works

```text
Your App -> Slack Bridge API -> Scriban Template -> Slack Incoming Webhook
```

1. An application sends an event such as `user_signup` or `payment_received`.
2. Slack Bridge authenticates the request with a Slack bot API key.
3. The event key is matched to an `EventDefinition` for the API key's Slack bot.
4. The configured Scriban template is rendered with the event data.
5. The rendered message is posted to the Slack bot's webhook, unless the event definition has its optional webhook override enabled.
6. Success or failure is written to `EventLogs`.

Inbound slash commands use the opposite direction:

```text
Slack Slash Command -> Slack Bridge -> Downstream App Callback -> Slack Response
```

1. Slack posts an `application/x-www-form-urlencoded` slash command to `POST /api/slack/commands`.
2. Slack Bridge verifies `X-Slack-Signature` and `X-Slack-Request-Timestamp` with the configured route signing secret.
3. The command name and optional Slack team ID are matched to an active `SlackCommandRoute`.
4. Slack Bridge forwards a normalized JSON payload to the route's downstream URL.
5. A downstream Slack JSON response is relayed, a 204 becomes an empty 200, and failures become a safe ephemeral fallback.
6. The request outcome is written to `SlackCommandLogs`.

## Example

Request:

```http
POST /api/events
x-api-key: sb_your_bot_key
Content-Type: application/json
```

```json
{
  "key": "user_signup",
  "data": {
    "email": "user@example.com",
    "plan": "pro"
  }
}
```

Template:

```scriban
New signup!
Email: {{ email }}
Plan: {{ plan }}
```

Slack message:

```text
New signup!
Email: user@example.com
Plan: pro
```

## Slash Command Gateway

Slack command request URL:

```http
POST /api/slack/commands
Content-Type: application/x-www-form-urlencoded
X-Slack-Signature: v0=...
X-Slack-Request-Timestamp: 1781270000
```

Create routes in **Slack commands**. Each route belongs to a Slack bot/project and configures:

- slash command name, such as `/shoppingtajm`
- Slack signing secret, encrypted at rest
- downstream callback URL
- downstream auth header and encrypted shared secret
- optional allowed Slack team ID
- active/inactive state

Slack Bridge forwards this JSON to the downstream app:

```json
{
  "type": "slash_command",
  "teamId": "T123",
  "teamDomain": "example",
  "channelId": "C123",
  "channelName": "general",
  "userId": "U123",
  "userName": "deprecated_from_slack_if_present",
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

Downstream apps may return `200` with a Slack JSON response or `204` for an empty acknowledgement. `4xx`, `5xx`, invalid JSON, timeout, or transport failures are converted to:

```json
{
  "response_type": "ephemeral",
  "text": "Kommandot togs emot men kunde inte behandlas just nu."
}
```

## Features

- API key authentication per Slack bot
- Hashed API key storage
- Configurable Slack bots, API keys, and event definitions
- Configurable inbound Slack slash command routes
- Slack request signature and timestamp verification
- Downstream slash command forwarding with fallback Slack responses
- Dynamic Scriban message templates
- Slack bot webhook delivery
- Optional event-level Slack webhook override, disabled by default
- Razor Pages admin dashboard
- ASP.NET Core Identity login/logout and first-admin setup
- Admin and Member roles
- Event logs for debugging and traceability
- Inbound slash command logs for validation, routing, and downstream outcomes
- Monthly usage tracking
- Local plan enforcement for Free, Pro, and Scale plans
- Background retry worker for failed Slack deliveries
- Single-tenant customer instance model, ready to evolve toward multi-tenancy

## Tech Stack

- .NET 9 / ASP.NET Core
- Razor Pages and Web API controllers
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- Scriban
- Slack Incoming Webhooks
- Slack Slash Commands

Stripe is intentionally skipped for now. Billing is represented by a local `Subscription` record behind `IBillingService`, so Stripe Checkout and webhooks can be added later without changing the event ingestion pipeline.

## Architecture

Slack Bridge is a modular monolith. The web app contains the API, admin UI, Identity, persistence, and background worker in one deployable unit.

Core services:

- `ISlackService` posts rendered messages to Slack.
- `ISlackRequestVerifier` validates Slack signed requests.
- `ISlackCommandGatewayService` routes verified slash command payloads.
- `IDownstreamSlackCommandClient` forwards normalized command JSON to downstream apps.
- `ITemplateService` renders Scriban templates from arbitrary JSON.
- `IApiKeyValidator` validates hashed API keys.
- `IEventIngestionService` coordinates event handling.
- `IEventLogService` writes delivery logs.
- `IUsageService` tracks monthly usage and enforces limits.
- `IBillingService` manages the local subscription record.
- `FailedSlackRetryWorker` retries failed Slack sends.

Single-tenancy is explicit through `CustomerInstanceId`. Today each deployment uses one customer instance. Later, the same shape can become true multi-tenancy with scoped queries and tenant resolution.

## Project Structure

```text
SlackBridge.Web/
  Controllers/
    EventsController.cs
    SlackCommandsController.cs
  Contracts/
    EventRequest.cs
    EventResponse.cs
    SlackCommandEnvelope.cs
  Data/
    SlackBridgeDbContext.cs
    Migrations/
  Models/
    ApplicationUser.cs
    ApiKey.cs
    CustomerInstance.cs
    EventDefinition.cs
    EventLog.cs
    EventLogStatus.cs
    PlanType.cs
    Project.cs
    RetryState.cs
    SlackCommandLog.cs
    SlackCommandLogStatus.cs
    SlackCommandRoute.cs
    Subscription.cs
    UsageMetric.cs
  Pages/
    Account/
    Admin/
      ApiKeys/
      Billing/
      EventDefinitions/
      Logs/
      Projects/
      SlackCommandRoutes/
      Usage/
  Services/
    ApiKeyGenerator.cs
    ApiKeyValidator.cs
    BillingService.cs
    CustomerInstanceContext.cs
    EventIngestionService.cs
    EventLogService.cs
    FailedSlackRetryWorker.cs
    PlanLimits.cs
    SlackCommandForwarder.cs
    SlackCommandGatewayService.cs
    SlackRequestVerifier.cs
    SlackService.cs
    TemplateService.cs
    UsageService.cs
```

## Run Locally

```powershell
dotnet restore
dotnet ef database update --project SlackBridge.Web --startup-project SlackBridge.Web
dotnet run --project SlackBridge.Web
```

The default connection string uses SQL Server LocalDB and creates a `SlackBridge` database.

Open the setup page after first run:

```text
http://localhost:5019/Account/Register
```

Create the first admin user, then open **Slack bots** and configure:

1. A Slack bot with a Slack webhook URL
2. An API key inside that Slack bot
3. Event definitions inside that Slack bot
4. Scriban templates for those events

Event definitions use the Slack bot webhook by default. Enable the event-level custom webhook only when one event must route to a different Slack destination.

For slash commands, open **Slack commands**, create a route, and set the Slack app slash command Request URL to:

```text
https://your-slack-bridge.example.com/api/slack/commands
```

Keep Shoppingtajm-style business logic in the downstream app. Slack Bridge only verifies, normalizes, forwards, relays, falls back, and logs. See [docs/slack-slash-commands.md](docs/slack-slash-commands.md) for the downstream contract.

## Plans

Plan enforcement is local for now:

- Free: 500 events/month, 2 API keys, 1 Slack bot
- Pro: 25,000 events/month, 20 API keys, 25 Slack bots
- Scale: 250,000 events/month, 100 API keys, 250 Slack bots

The billing page updates the local subscription plan. Stripe integration can be added later behind `IBillingService`.

## Use Cases

- New user signups
- Payments and billing events
- System alerts
- Admin notifications
- Internal product analytics
- CI/CD and deployment updates

## Roadmap

- Slack Bot support via `chat.postMessage`
- Interactive callbacks, modals, and App Home support
- Multi-channel routing rules
- Rich Slack Block Kit templates
- Plugin system for Email, Discord, and generic webhooks
- Stripe Checkout and subscription webhooks
- Multi-tenant hosting model

## Philosophy

Make useful notifications easy enough that teams actually keep them.
