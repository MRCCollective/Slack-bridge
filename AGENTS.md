# AGENTS.md

## Scope

This repo contains Slack Bridge: one ASP.NET Core .NET 9 single-tenant SaaS-ready app with Web API endpoints, Razor Pages admin UI, ASP.NET Core Identity, EF Core SQL Server persistence, Scriban templates, usage limits, Slack Incoming Webhook delivery, and a minimal inbound Slack slash command gateway.

## Commands

```powershell
dotnet restore
dotnet build SlackBridge.sln
dotnet ef database update --project SlackBridge.Web --startup-project SlackBridge.Web
dotnet run --project SlackBridge.Web
```

## Conventions

- Keep orchestration in services, not controllers or page models.
- Use dependency injection for app services and HTTP clients.
- Store API keys as hashes only.
- Store reusable Slack signing secrets and downstream shared secrets encrypted, not plain text.
- Log every API event attempt to `EventLogs`.
- Log inbound slash command validation/routing/downstream outcomes to `SlackCommandLogs`.
- Keep customer-scoped data on `CustomerInstanceId` even though this is single-tenant today.
- Enforce plan limits through `IUsageService`.
- Keep SlackBridge focused on Slack-shaped adapter concerns. Downstream apps own slash command business logic.
- Stripe is intentionally skipped for now; keep billing changes behind `IBillingService`.
- Keep Razor Pages simple and server-rendered.
- Prefer focused EF migrations over manual SQL.
