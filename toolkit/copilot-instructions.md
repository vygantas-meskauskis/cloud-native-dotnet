# Copilot instructions

<!--
Template for repository custom instructions for GitHub Copilot.
Copy this file to .github/copilot-instructions.md in your repository and edit it.
Copilot uses it as extra context for requests in this repository (🔧 check the GitHub docs for which Copilot surfaces read it).
Keep it short and specific: rules the model can't guess from the code. Delete what doesn't apply.
-->

This repository contains a .NET 10 (LTS) backend: ASP.NET Core minimal APIs, EF Core 10 on Azure SQL, Azure Service Bus and Aspire for local orchestration. It deploys to Azure Container Apps with Bicep and GitHub Actions.

## Build and test

- Build: `dotnet build Ledgerly.slnx` from `sample/`. Warnings are errors; fix them, never suppress them without a comment explaining why.
- Test: `dotnet test --solution Ledgerly.slnx`. Tests run on Microsoft.Testing.Platform, not VSTest. Don't add `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` or `coverlet.collector`.
- Integration tests need SQL Server: Docker (Testcontainers) or the `LEDGERLY_TEST_SQL` environment variable.
- Run the whole system locally with `aspire run`.

## Packages

- Versions live only in `Directory.Packages.props` (central package management). Never put `Version=` on a `PackageReference`.
- Don't add a package when the base class library or an existing package already does the job. Ask first for new dependencies.

## C# style

- C# 14 with nullable reference types on. File-scoped namespaces, primary constructors and records where they fit.
- `async` all the way with `CancellationToken` passed through. Never `.Result`, `.Wait()` or `async void`.
- Inject `TimeProvider` instead of using `DateTime.Now` or `DateTime.UtcNow`.
- Money and quantities are `decimal`. Parse and format with `CultureInfo.InvariantCulture` for machine data.
- Log with `[LoggerMessage]` source-generated methods and message templates. Never log secrets, tokens or personal data.

## API conventions

- Minimal APIs in `Endpoints/*.cs`, one static class per area, using route groups and `TypedResults`.
- Errors are `ProblemDetails`. Validation errors return 400, concurrency conflicts 409, missing or not-owned resources 404.
- Every endpoint that touches user data requires authorization and filters by the current user's id.

## Data

- Add new entities through their `DbSet` (`db.Transactions.Add(...)`), not through a navigation collection: keys are client-generated with `Guid.CreateVersion7()`.
- Schema changes need a migration: `dotnet ef migrations add <Name> --project src/Ledgerly.Core --output-dir Data/Migrations`. Migrations must stay compatible with the previous app version (expand, then contract).
- Read-only queries use `AsNoTracking()` or projections.

## Messaging

- Never publish to Service Bus directly from a request. Write an outbox message in the same `SaveChanges` call; the worker relays it.
- Consumers must be idempotent: check `ProcessedMessages` before doing work.

## Infrastructure and workflows

- Azure resources are defined only in `sample/infra/*.bicep`. Run `bicep lint` and `bicep build` after changes.
- Use managed identities and RBAC role assignments. Never add connection strings with keys or passwords.
- GitHub Actions use least-privilege `permissions:` and OIDC for Azure. Never add cloud credentials as secrets.

## When you're not sure

- Say so, and point to the file or doc you would check. Don't invent APIs or package versions.
- Prefer small, reviewable changes with a test that fails without the change.
