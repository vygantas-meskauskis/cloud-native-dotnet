# Cloud-Native .NET

**Modern .NET, GitHub and Azure for enterprise apps.** A self-study course for senior .NET developers who built their career on .NET Framework, IIS, Remote Desktop and log files, and now want the modern stack: .NET 10, Aspire, GitHub Actions, Azure Container Apps, OpenTelemetry and the reliability habits that come with them.

> 📅 Written in October 2026 for .NET 10 (LTS), C# 14, Aspire 13 and the Azure services of that time. Facts marked 🔧 change often; check the linked docs.

---

## What "cloud-native .NET" means here

You already know how to build good software. What changed is **where** it runs and **how** you know it works.

| You know this | This course gets you to |
|---|---|
| ASP.NET MVC / Web API 2 on IIS | ASP.NET Core 10 minimal APIs in containers on Azure Container Apps |
| `web.config` and passwords in config | Environment configuration, managed identities and Key Vault |
| Thursday-night deploys over RDP | CI on every pull request, CD on every merge, with OIDC and no stored secrets |
| Windows Services, SQL Agent jobs, MSMQ | Background workers, Azure Service Bus, the outbox pattern |
| Log files on three servers | OpenTelemetry traces, metrics and logs, queried with KQL |
| "The site is up" | SLIs, SLOs, error budgets and actionable alerts |
| Clicking in the portal | Bicep in git, linted in CI |

## How the guide works

You join **Ledgerly**, a small fintech building a portfolio tracker, as a senior .NET developer. Each chapter opens with a problem the team hits, then explains the concepts, shows real code from a working sample, and ends with traps, copy-ready helpers and an optional exercise.

| Section in every chapter | What it gives you |
|---|---|
| 📖 Story | The problem, as it shows up at Ledgerly |
| 🏚️ Old way → 🚀 New way | Your old habit next to the modern practice |
| 🧠 Concepts | The ideas, in tables and decision guides |
| 💻 In Ledgerly | Real code from the sample |
| 🗺️ Diagrams | Who does what, where things live, how it flows |
| ⚠️ Common traps | Mistakes that are easy to make |
| 🧰 Helpers | Copy-ready snippets and "make it permanent" advice |
| 🧪 Try it yourself | Optional hands-on steps |
| ✅ Recap | Three lines to remember |

| Marker | Meaning |
|---|---|
| 🔧 | Volatile fact (version, limit, date, SKU name). Check the docs before relying on it. |
| 💰 | Has a cost impact. The course explains cost drivers, not prices. |
| 🧪 Not run by the author | Azure steps written from the docs and validated with `bicep build`, but not deployed end to end. |

## The sample

The repository contains **Ledgerly**, a runnable .NET 10 system the chapters quote line by line:

| Part | What it is |
|---|---|
| `sample/src` | API, background worker, fake market-data vendor, migration runner, shared core, service defaults and the Aspire AppHost |
| `sample/tests` | 15 unit tests and 8 integration tests against a real SQL Server |
| `sample/infra` | Bicep for Azure Container Apps, Azure SQL, Service Bus, Azure Managed Redis, Key Vault, Container Registry and Application Insights |
| `.github/workflows` | CI (build, test, container images, Bicep lint), CodeQL, and a deploy workflow that stays off until you connect your own Azure subscription |

Browse it on [GitHub](https://github.com/vygantas-meskauskis/cloud-native-dotnet/tree/main/sample).

## Tools you need

| Tool | Why | Required? |
|---|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Build and run everything | Yes |
| Git and a GitHub account | Chapters 8, 9 and 12 | Yes |
| An editor: Visual Studio 2026, VS Code with C# Dev Kit, or Rider | Writing code | Yes |
| Docker Desktop or Podman | Aspire runs SQL Server, Redis and the Service Bus emulator as containers | For running the whole system locally |
| Aspire CLI | `aspire run` (or run the AppHost project with `dotnet run`) | Recommended |
| Azure subscription and Azure CLI | Chapters 10 to 13 and 16, if you deploy | Optional |
| GitHub Copilot | Chapter 20 | Optional |

The whole course can be read without an Azure subscription. Everything up to the deploy runs on your laptop.

## Contents

### Part 1 · Foundations

1. [From legacy to cloud-native](chapters/01-from-legacy-to-cloud-native.md): the mental model, the Ledgerly architecture and the course map
2. [Modern .NET in one sitting](chapters/02-modern-dotnet-in-one-sitting.md): what changed from .NET Framework 4.8 to .NET 10 and C# 14
3. [A solution set up like a pro](chapters/03-a-solution-set-up-like-a-pro.md): central package management, analyzers, warnings as errors, one-command builds

### Part 2 · Build, ship, run

4. [Designing the API](chapters/04-designing-the-api.md): minimal APIs, validation, ProblemDetails, OpenAPI, CORS, rate limiting
5. [Data: EF Core and Azure SQL](chapters/05-data-ef-core-and-azure-sql.md): EF Core 10, migrations as a deploy step, concurrency, Azure SQL serverless
6. [Local development with Aspire](chapters/06-local-dev-with-aspire.md): the AppHost, containers, the dashboard and service defaults
7. [Background work and messaging](chapters/07-background-work-and-messaging.md): workers, Service Bus, the outbox, idempotency, dead-lettering
8. [GitHub as the team's home](chapters/08-github-as-the-teams-home.md): GitHub flow, rulesets, CODEOWNERS, Dependabot, Copilot code review
9. [CI with GitHub Actions](chapters/09-ci-with-github-actions.md): build, test, container images without Dockerfiles, CodeQL
10. [Choosing Azure services](chapters/10-choosing-azure-services.md): App Service vs Container Apps vs AKS vs Functions, data, messaging, caching, cost
11. [Infrastructure as code](chapters/11-infrastructure-as-code.md): Bicep modules, role assignments, what-if, `azd` and `aspire deploy`
12. [CD to Azure with OIDC](chapters/12-cd-to-azure-with-oidc.md): federated credentials, environments, build once, migrations job, rollback
13. [Identity and secrets](chapters/13-identity-and-secrets.md): managed identity everywhere, Entra auth to SQL and Service Bus, JWT bearer
14. [Reliability patterns](chapters/14-reliability-patterns.md): retries, timeouts, circuit breakers, graceful degradation, health probes
15. [Observability I: logging and OpenTelemetry](chapters/15-observability-logging-and-opentelemetry.md): structured logs, traces, metrics, context through queues
16. [Observability II: Azure Monitor, KQL and SLOs](chapters/16-observability-azure-monitor-kql-slos.md): Application Insights, KQL, alerts, error budgets

### Part 3 · Everyday practice

17. [Testing strategy](chapters/17-testing-strategy.md): unit and integration tests, Testcontainers, Microsoft.Testing.Platform
18. [Performance and scale](chapters/18-performance-and-scale.md): HybridCache, async all the way, EF performance, scale rules, load tests
19. [Running production](chapters/19-running-production.md): on-call, runbooks, kill switches, safe deploys, postmortems, cost reviews
20. [AI-assisted .NET development](chapters/20-ai-assisted-dotnet-development.md): GitHub Copilot like a senior, custom instructions, MCP, guardrails

### Part 4 · Toolkit

21. [Your toolkit](chapters/21-your-toolkit.md): the habits notebook closed, the day-one checklist, what to learn next

Plus the [toolkit](toolkit/README.md) (cheat sheet, checklists, templates and config files) and the [glossary](GLOSSARY.md).

## Pace

| Pace | Plan |
|---|---|
| Steady | Two chapters a week, about 10 weeks, doing most "Try it yourself" sections |
| Fast | One part per week, about 4 weeks, reading only, trying the sample at the end |
| Reference | Read chapters 1, 2 and 21 first, then jump to whatever you need this week |

Each chapter takes 20 to 40 minutes to read. Exercises add 15 to 60 minutes.

## Run the sample in two minutes

```bash
git clone https://github.com/vygantas-meskauskis/cloud-native-dotnet.git
cd cloud-native-dotnet/sample
dotnet build Ledgerly.slnx
dotnet test --solution Ledgerly.slnx   # integration tests need Docker, or set LEDGERLY_TEST_SQL
aspire run                             # needs Docker Desktop or Podman
```

**Start → [Chapter 1: From legacy to cloud-native](chapters/01-from-legacy-to-cloud-native.md)**
