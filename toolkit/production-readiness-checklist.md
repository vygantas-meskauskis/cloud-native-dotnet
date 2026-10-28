# Production readiness checklist

Go through this before a service takes real traffic, and again once a quarter. Every "no" is either fixed or written down as an accepted risk, with an owner.

> Copy this into an issue (one checkbox per line) and work through it with the team. It takes about an hour for a small service.

## 1. Build and supply chain

- [ ] The build is reproducible from a clean clone: `global.json` pins the SDK, `nuget.config` lists the package sources.
- [ ] Warnings are errors. Analyzers and code style run in the build.
- [ ] Central package management is on, with transitive pinning for anything NuGet audit flags.
- [ ] Dependabot (or similar) opens update PRs for packages and GitHub Actions.
- [ ] CodeQL or another SAST tool runs on pull requests.
- [ ] Secret scanning and push protection are on for the repository.
- [ ] `main` is protected: pull request required, CI required, no force pushes.

## 2. Tests

- [ ] Unit tests cover the domain rules that lose money when wrong.
- [ ] Integration tests run against the real database engine, not an in-memory fake.
- [ ] Tests assert their own setup (no tests that pass for the wrong reason).
- [ ] CI fails when zero tests run.
- [ ] Flaky tests are fixed or quarantined with an issue, not retried silently.

## 3. Deploy and rollback

- [ ] Deploys run from CI only. Nobody has standing write access to production "just in case".
- [ ] CI authenticates to Azure with OIDC. No cloud credentials are stored in GitHub secrets.
- [ ] One image is built per commit and tagged with the commit SHA. The same image goes to every environment.
- [ ] Database migrations run as a separate step before the new app version starts.
- [ ] Schema changes are backward compatible (expand, deploy, contract).
- [ ] A smoke test runs after each deploy and fails the workflow.
- [ ] Rollback is written down and has been practiced once.

## 4. Identity and secrets

- [ ] Services use managed identities for Azure SQL, Service Bus, Key Vault and the container registry.
- [ ] Local (key or password) authentication is disabled where the service allows it.
- [ ] The few remaining secrets live in Key Vault and reach the app as secret references.
- [ ] Each identity has only the roles it needs, scoped to the resource, not the subscription.
- [ ] User-facing endpoints require authentication. Authorization checks ownership (return 404 for other users' data).
- [ ] CORS allows only known origins.

## 5. Reliability

- [ ] Every outbound HTTP call has a timeout, retries with jitter and a circuit breaker.
- [ ] The service degrades gracefully when a dependency is down. You know what the user sees.
- [ ] The liveness probe checks only the process. The readiness probe checks dependencies.
- [ ] Message consumers are idempotent. Duplicate deliveries are harmless.
- [ ] Poison messages go to the dead-letter queue. Someone is alerted, and there is a way to replay them.
- [ ] Graceful shutdown finishes in-flight work within the platform's grace period.
- [ ] Rate limits protect expensive endpoints and return 429.
- [ ] Min and max replicas are set on purpose. Cold starts are acceptable or avoided.

## 6. Observability

- [ ] Logs are structured, exported with OpenTelemetry, and contain no secrets or personal data.
- [ ] Traces flow end to end, including across queues.
- [ ] Each service has a clear `service.name`.
- [ ] Business metrics exist for the things the product manager asks about.
- [ ] SLIs and SLOs are defined for the main user journeys (see the SLO worksheet).
- [ ] Every alert is actionable and links to a runbook.
- [ ] Sampling and the log daily cap are set on purpose, and you know what gets dropped.

## 7. Data

- [ ] Backups and point-in-time restore are enabled, and the retention period is known.
- [ ] A restore has been tested at least once.
- [ ] Concurrency conflicts are handled (409), not silently overwritten.
- [ ] Personal data is identified. You know where it lives and how to delete it.

## 8. Cost

- [ ] Every resource has tags (owner, environment, cost center).
- [ ] A budget with alerts exists for the resource group or subscription. Remember that budgets notify, they don't stop spending.
- [ ] The main cost drivers are known: replicas, database compute, log ingestion.

## 9. People

- [ ] An owner and an on-call rotation exist.
- [ ] Runbooks exist for the top alerts.
- [ ] Architecture decisions are recorded as ADRs.
- [ ] The postmortem process is agreed before the first incident, not after.
