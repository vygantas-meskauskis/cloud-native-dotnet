# Glossary

Terms used in the course, in plain language, with the chapter that explains them. 🔧 marks names that change often.

| Term | Meaning | Chapter |
|---|---|---|
| **ACA** | Azure Container Apps: a serverless platform that runs containers with scale rules, revisions and jobs, without you running a Kubernetes cluster. | 10 |
| **ACR** | Azure Container Registry: a private registry for your container images. | 11, 12 |
| **Activity / ActivitySource** | .NET's API for OpenTelemetry spans. An `ActivitySource` creates `Activity` objects; each one is a span in a trace. | 15 |
| **ADR** | Architecture decision record: a short document capturing one significant decision, its context and consequences. | 1, 19 |
| **AKS** | Azure Kubernetes Service: managed Kubernetes. Full control, more to operate. | 10 |
| **Analyzer** | A Roslyn component that checks code during the build and reports diagnostics, such as IDE0005 or CA2007. | 3 |
| **AppHost** | The Aspire project that describes the whole system in C# and starts it locally. | 6 |
| **Application Insights** | The Azure Monitor feature for application telemetry: requests, dependencies, traces, exceptions and metrics. Stores data in a Log Analytics workspace. | 16 |
| **Aspire** 🔧 | Microsoft's stack for building and running distributed apps: the AppHost, service defaults, integrations and the dashboard. Formerly ".NET Aspire". | 6 |
| **Aspire dashboard** | A local web UI that shows resources, console logs, structured logs, traces and metrics from everything the AppHost runs. | 6, 15 |
| **Availability (SLI)** | The share of valid requests that succeed. | 16 |
| **Azure Managed Redis** 🔧 | The current managed Redis offering in Azure. Use it for new work instead of Azure Cache for Redis, which is being retired. | 10 |
| **azd** | The Azure Developer CLI: provisions and deploys an app from a template with a few commands. | 11 |
| **Bicep** | A declarative language for Azure resources that compiles to ARM templates. | 11 |
| **Blameless postmortem** | An incident review that looks for system and process causes, not people to blame. | 19 |
| **Build once, deploy many** | Build one immutable image per commit and promote the same image through every environment. | 12 |
| **Burn rate** | How fast an error budget is being spent, relative to spending it evenly over the SLO window. | 16 |
| **Central package management (CPM)** | One `Directory.Packages.props` file holds the versions of every NuGet package in the repo. | 3 |
| **Circuit breaker** | A resilience strategy that stops calling a failing dependency for a while so it can recover and your threads aren't wasted. | 14 |
| **CODEOWNERS** | A GitHub file that maps paths to the people or teams who must review changes to them. | 8 |
| **CodeQL** | GitHub's static analysis engine for finding security bugs. | 9 |
| **Cold start** | The delay when a new replica starts to serve a request, for example after scaling from zero. | 18 |
| **Container Apps environment** | The boundary around a group of container apps: shared network, logging and scale. | 10, 11 |
| **Container Apps job** | A container that runs to completion, started manually, on a schedule or by an event. | 7, 12 |
| **Correlation / trace context** | IDs (W3C `traceparent`) passed between services so their telemetry joins into one trace. | 15 |
| **CORS** | Cross-origin resource sharing: browser rules that decide which other origins (like a React SPA) may call your API. | 4, 13 |
| **Dead-letter queue (DLQ)** | A sub-queue where Service Bus moves messages that can't be processed, for inspection and replay. | 7, 19 |
| **DefaultAzureCredential** | An Azure.Identity credential that tries several sources in order (environment, managed identity, developer tools). Convenient locally; prefer `ManagedIdentityCredential` in production. | 13 |
| **Dependabot** | GitHub's bot that opens pull requests to update dependencies and GitHub Actions. | 8, 9 |
| **Duplicate detection** | A Service Bus feature that drops messages with a `MessageId` already seen within a time window. | 7 |
| **EF Core** | Entity Framework Core, the modern .NET ORM. Not the same codebase as EF6. | 5 |
| **Entra ID** | Microsoft Entra ID, formerly Azure Active Directory: Microsoft's identity platform for users and workloads. | 13 |
| **Environment (GitHub)** | A named deploy target in GitHub with its own protection rules, variables and secrets. | 12 |
| **Error budget** | 1 minus the SLO: how much unreliability is allowed in the window. | 16 |
| **Execution strategy** | EF Core's retry policy for transient database errors. | 5, 14 |
| **Expand and contract** | A migration approach where you add new schema first, deploy code that uses it, then remove the old schema later. Keeps deploys zero-downtime. | 12 |
| **Feature flag** | A runtime switch that turns behavior on or off without a deploy. Used as a kill switch during incidents. | 4, 19 |
| **Federated credential** | A trust in Entra ID that accepts tokens from an external issuer (like GitHub Actions) for a specific subject, instead of a secret. | 12 |
| **GitHub Actions** | GitHub's CI/CD system: YAML workflows in `.github/workflows`. | 9 |
| **GitHub flow** | Branch from `main`, open a pull request, review, merge, deploy. | 8 |
| **Graceful degradation** | Returning a reduced but useful answer when a dependency fails, instead of an error. | 14 |
| **Graceful shutdown** | Finishing in-flight work when the platform sends a stop signal, within the grace period. | 7, 14 |
| **Health probe** | A platform check of your app. **Startup**: has it started? **Liveness**: is the process alive (restart if not)? **Readiness**: can it take traffic? | 14 |
| **HybridCache** | A .NET caching API that combines an in-memory cache with an optional distributed cache and protects against cache stampedes. | 18 |
| **Idempotency** | Doing the same operation twice has the same effect as doing it once. Required for message consumers. | 7, 14 |
| **IHttpClientFactory** | The .NET factory that manages `HttpClient` lifetimes and lets you add handlers like resilience. | 2, 14 |
| **KEDA** | Kubernetes Event-driven Autoscaling. Container Apps uses KEDA scalers, for example to scale on Service Bus queue length. | 18 |
| **Kestrel** | The cross-platform web server built into ASP.NET Core. Replaces IIS as the process that serves requests. | 2 |
| **Key Vault** | Azure's service for secrets, keys and certificates. | 13 |
| **KQL** | Kusto Query Language, used to query logs in Azure Monitor and Log Analytics. | 16 |
| **Least privilege** | Every identity gets only the permissions it needs, on the smallest scope. | 11, 13 |
| **Log Analytics workspace** | The Azure Monitor store for logs and Application Insights data, queried with KQL. | 16 |
| **Managed identity** | An identity for an Azure resource, managed by Entra ID. Your code gets tokens without any secret. **User-assigned** identities are separate resources you can share and grant roles before the app exists. | 13 |
| **Message template** | A log message with named placeholders (`"Import {ImportId} done"`) whose values are stored as structured properties. | 15 |
| **Meter** | .NET's API for OpenTelemetry metrics: counters, histograms and gauges. | 15 |
| **Microsoft.Testing.Platform (MTP)** | The modern .NET test runner that replaces VSTest. Opt in through `global.json` on the .NET 10 SDK. | 17 |
| **Minimal APIs** | ASP.NET Core's lightweight way to define HTTP endpoints with lambdas or static methods instead of controllers. | 4 |
| **Migration (EF)** | Generated code that changes the database schema from one model version to the next. | 5 |
| **OIDC** | OpenID Connect. In CI/CD, GitHub issues a short-lived token that Azure trusts through a federated credential, so no cloud secret is stored. | 12 |
| **OpenAPI** | A standard machine-readable description of an HTTP API. ASP.NET Core 10 generates OpenAPI 3.1 by default. | 4 |
| **OpenTelemetry (OTel)** | The vendor-neutral standard and SDKs for traces, metrics and logs. | 15 |
| **OTLP** | The OpenTelemetry protocol used to send telemetry to a collector or backend, such as the Aspire dashboard. | 15 |
| **Optimistic concurrency** | Detecting conflicting updates with a version column (`rowversion`) and rejecting the second writer (409) instead of locking. | 5 |
| **Outbox (transactional)** | Writing the message to publish into the same database transaction as the business change, then relaying it to the broker. No lost or phantom messages. | 7 |
| **Peek-lock** | The Service Bus receive mode where a message is locked, not removed, until the consumer completes, abandons or dead-letters it. | 7 |
| **ProblemDetails** | The standard JSON shape for HTTP API errors (RFC 9457). | 4 |
| **Rate limiting** | Limiting how many requests a client can make in a window. Should return 429 Too Many Requests. | 4, 14 |
| **RBAC** | Role-based access control. In Azure, role assignments grant an identity a role on a scope. | 11, 13 |
| **Replica** | One running instance of a container app. | 10, 18 |
| **Revision** | An immutable snapshot of a container app version in Azure Container Apps. | 12 |
| **Ruleset** | GitHub's newer, layered way to protect branches and tags, which can apply across repositories. | 8 |
| **Run-to-completion** | A process that does one job and exits, like the migration service. | 5, 12 |
| **Sampling** | Keeping only a share of traces to control volume and cost. | 15, 16 |
| **Scale rule** | A Container Apps rule that adds or removes replicas based on HTTP concurrency, queue length or other signals. | 18 |
| **Scalar** | An interactive UI for browsing and calling an OpenAPI document. | 4 |
| **Service Bus** | Azure's enterprise message broker: queues and topics with sessions, dead-lettering and duplicate detection. | 7 |
| **Service defaults** | A shared Aspire project that wires OpenTelemetry, health checks, resilience and service discovery into every service with one call. | 6 |
| **Service discovery** | Resolving a logical name like `https+http://marketdata` to a real address from configuration. | 6 |
| **SLI** | Service level indicator: a measured ratio of good events to valid events. | 16 |
| **SLO** | Service level objective: a target for an SLI over a time window. | 16 |
| **Span** | One timed operation in a trace, with attributes and a parent. | 15 |
| **Standard resilience handler** | `AddStandardResilienceHandler()`: a ready-made pipeline of rate limiter, timeouts, retries and circuit breaker for `HttpClient`. | 14 |
| **Structured logging** | Logs as events with named properties, not just text lines, so you can query them. | 15 |
| **Testcontainers** | A library that starts real dependencies (like SQL Server) in throwaway containers for tests. | 17 |
| **TimeProvider** | The .NET abstraction for the current time and timers. Fake it in tests with `FakeTimeProvider`. | 2, 17 |
| **Trace** | The tree of spans for one request as it flows through every service. | 15 |
| **Transitive pinning** | A CPM setting that lets you force the version of a package you don't reference directly, for example to fix a vulnerability. | 3 |
| **TypedResults** | ASP.NET Core helpers that return strongly typed HTTP results, which also feed the OpenAPI document. | 4 |
| **What-if** | An Azure deployment preview that shows which resources would change, before you deploy. | 11 |
| **Workload profile** | The compute type of a Container Apps environment, such as Consumption. | 10 |
| **xUnit v3** | The current major version of the xUnit test framework, running on Microsoft.Testing.Platform. | 17 |
