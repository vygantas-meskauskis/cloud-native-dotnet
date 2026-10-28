# Cheat sheet: old to new

Each row maps an old .NET Framework or IIS habit to its modern replacement, with the chapter that covers it.

## Runtime and code

| You used to | Now you | Chapter |
|---|---|---|
| Target .NET Framework 4.8, installed on the server | Target `net10.0` (LTS). The runtime ships inside your container image. | 2 |
| `Global.asax`, `Startup`, OWIN | `WebApplication.CreateBuilder(args)` in `Program.cs`. One composition root. | 2 |
| Unity, Autofac or Ninject for DI | Built-in `IServiceCollection`. Add a container only if you really need its extra features. | 2 |
| `ConfigurationManager.AppSettings["x"]` | `IConfiguration` plus the options pattern (`IOptions<T>`), with environment variables overriding JSON | 2 |
| `web.config` transforms per environment | `appsettings.{Environment}.json`, environment variables and Key Vault | 2, 13 |
| `new HttpClient()` per call, or one static client | `IHttpClientFactory` with typed clients and a resilience handler | 2, 14 |
| `.Result` and `.Wait()` on tasks | `async` all the way, passing `CancellationToken` | 2, 18 |
| `DateTime.Now` | `TimeProvider` (inject it, fake it in tests) | 2, 17 |
| `Guid.NewGuid()` for clustered keys | `Guid.CreateVersion7()` (time-ordered) | 2, 5 |
| Classes with 30 lines of properties | `record` types, primary constructors and `required` members | 2 |
| `string.Format` log messages | Message templates plus `[LoggerMessage]` source generation | 15 |

## Solution and build

| You used to | Now you | Chapter |
|---|---|---|
| `packages.config`, a version per project | Central package management in `Directory.Packages.props` | 3 |
| `.sln` files that merge-conflict | `.slnx` (XML solution format) | 3 |
| Compiler warnings nobody reads | `TreatWarningsAsErrors`, analyzers and code style enforced in build | 3 |
| A wiki page called "Developer setup" | `global.json`, a tool manifest and one `aspire run` | 3, 6 |
| Package sources configured per machine | `nuget.config` in the repo with `<clear/>` and package source mapping | 3, 13 |

## API

| You used to | Now you | Chapter |
|---|---|---|
| Web API 2 controllers | Minimal APIs with route groups and `TypedResults` (controllers still work) | 4 |
| Custom error JSON per team | `ProblemDetails` (RFC 9457 shape) everywhere | 4 |
| A Word document describing the API | An OpenAPI document generated at build/run time, browsed with Scalar | 4 |
| Last write wins | Optimistic concurrency with `rowversion`, returning 409 | 4, 5 |
| No limits until the server falls over | Rate limiting middleware returning 429 | 4, 14 |

## Data

| You used to | Now you | Chapter |
|---|---|---|
| EF6 or hand-written ADO.NET | EF Core 10 | 5 |
| SQL scripts run by hand on release night | EF migrations applied by a separate run-to-completion process | 5, 12 |
| SQL logins with passwords in `web.config` | Microsoft Entra authentication with managed identities, no passwords | 13 |
| A shared dev database | A local SQL Server container per developer, started by Aspire | 6 |

## Background work

| You used to | Now you | Chapter |
|---|---|---|
| A Windows Service | A `BackgroundService` in a worker container | 7 |
| A SQL Agent job | `PeriodicTimer` in a worker, or a scheduled Container Apps job | 7 |
| MSMQ | Azure Service Bus queues with peek-lock and dead-lettering | 7 |
| "Send the message, then save" (and hope) | The transactional outbox and an idempotent consumer | 7 |

## Source control and delivery

| You used to | Now you | Chapter |
|---|---|---|
| TFVC or a shared drive | Git and GitHub flow: short branches, pull requests, protected `main` | 8 |
| A build server someone configured in 2014 | GitHub Actions workflows in the repo | 9 |
| "Works on my machine" | CI builds and tests every pull request | 9 |
| Web Deploy or xcopy over RDP on Thursday night | CD with OIDC to Azure, after every green build on `main` | 12 |
| A service account password in the build server | OIDC federated credentials: no secret stored in GitHub | 12 |
| Rolling back by copying the old folder back | Redeploying the previous image tag, or activating a previous revision | 12, 19 |

## Hosting

| You used to | Now you | Chapter |
|---|---|---|
| IIS on a VM you patch | Azure Container Apps (or App Service, which is closer to the IIS model) | 10 |
| Clicking in IIS Manager or the portal | Bicep in git, linted in CI | 11 |
| App pool identity | User-assigned managed identity | 13 |
| Load balancer health check on `/ping.aspx` | Startup, liveness and readiness probes on `/alive` and `/health` | 14 |
| "Add another VM" | Scale rules: HTTP concurrency, queue length (KEDA), min and max replicas | 18 |

## Observability and operations

| You used to | Now you | Chapter |
|---|---|---|
| log4net/NLog text files on each server | Structured logs exported with OpenTelemetry to Application Insights | 15 |
| RDP and grep | KQL queries across every instance | 16 |
| Performance counters on one box | OpenTelemetry metrics (`Meter`) and dashboards | 15, 16 |
| Correlating logs by timestamp | Distributed traces with W3C trace context, even through queues | 15 |
| "The site is up" | SLIs, SLOs and error budgets | 16 |
| An alert for every exception | Few, actionable alerts with a runbook each | 16, 19 |
| A blame meeting after the outage | A blameless postmortem with action items | 19 |
| Comment out code to switch a feature off | Feature flags as kill switches | 19 |

## Testing

| You used to | Now you | Chapter |
|---|---|---|
| MSTest/NUnit with VSTest | xUnit v3 on Microsoft.Testing.Platform | 17 |
| Integration tests against a shared test database | `WebApplicationFactory<Program>` plus a throwaway SQL Server (Testcontainers) | 17 |
| Mocking `DateTime.Now` with a wrapper | `FakeTimeProvider` | 17 |
