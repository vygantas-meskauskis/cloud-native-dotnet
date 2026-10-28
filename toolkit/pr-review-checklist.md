# PR review checklist

Use it for every pull request, including ones written with AI help. The bar is the same. CI already checks formatting, warnings, tests and known vulnerabilities, so spend your attention on what tools can't see.

## Before you read the code

- [ ] The description says **what** changed and **why**, and links the issue.
- [ ] The PR is small enough to review in 30 minutes. If not, ask for a split.
- [ ] CI is green. Don't review red builds.

## Correctness

- [ ] The change does what the issue asked, and no more.
- [ ] Edge cases are handled: empty input, duplicates, zero, negative numbers, very large input, other cultures (decimal separators, dates).
- [ ] Concurrency: what happens if two requests do this at the same time?
- [ ] Money and quantities use `decimal`, with explicit rounding rules.
- [ ] Time comes from `TimeProvider`, not `DateTime.Now`.

## Failure

- [ ] Every outbound call has a timeout and a `CancellationToken`.
- [ ] Failures return the right status code with `ProblemDetails`, not a stack trace.
- [ ] Message handlers are idempotent.
- [ ] Nothing blocks on async code (`.Result`, `.Wait()`).

## Data

- [ ] Migrations are backward compatible with the version currently running.
- [ ] Queries are bounded (paging, `Take`) and use indexes that exist.
- [ ] Read-only queries use `AsNoTracking()` or projections.
- [ ] New entities are added through the `DbSet`, not through a navigation, when keys are client-generated.

## Security

- [ ] No secrets, connection strings with passwords, or tokens in code, config or tests.
- [ ] Endpoints that need authentication require it. Data access checks the current user.
- [ ] Input is validated at the edge.
- [ ] New packages are needed, maintained and from the allowed source.
- [ ] Logs don't contain personal data or secrets.

## Observability

- [ ] New code paths log with message templates (`[LoggerMessage]` for hot paths), not string concatenation.
- [ ] New outbound calls or background jobs show up in traces.
- [ ] If the product manager will ask "how many?", there is a metric.
- [ ] Metric tags have low cardinality (no user IDs, no free text).

## Tests

- [ ] New behavior has tests at the right level (unit for rules, integration for HTTP + SQL).
- [ ] Tests assert their setup succeeded, not just the final result.
- [ ] A bug fix comes with a test that fails without the fix.

## AI-assisted changes

- [ ] The author can explain every line.
- [ ] APIs used actually exist in the versions we target (check the docs, not the model's memory).
- [ ] No silent scope creep: unrelated "improvements" are removed or split out.
- [ ] Generated tests check real behavior, not the implementation copied back.

## Review etiquette

- Prefix optional comments with "nit:".
- Ask questions instead of giving orders: "What happens if the vendor times out here?"
- Approve when it's better than `main` and safe, not when it's how you would have written it.
