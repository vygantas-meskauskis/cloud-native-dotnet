<!--
# Pull request template

Copy this file to .github/pull_request_template.md. GitHub pre-fills the description of every new pull request with it.
HTML comments like this one are hidden when the PR description is rendered, so you can keep the hints.
-->

## What and why

<!-- What does this change do, and why now? Link the issue: "Closes #123". -->

## How to verify

<!-- Steps a reviewer can follow, or the tests that prove it. -->

## Risk

- [ ] Database migration (backward compatible with the running version?)
- [ ] Configuration or infrastructure change (Bicep, app settings, roles)
- [ ] New package or version bump
- [ ] Behind a feature flag: `FlagName`
- [ ] None of the above

## Observability

<!-- New logs, metrics or traces? How will we know in production that this works or breaks? -->

## Checklist

- [ ] Tests added or updated, and they fail without this change
- [ ] No secrets, personal data or connection strings with passwords
- [ ] API changes reflected in the OpenAPI document (and the frontend team told)
- [ ] I can explain every line, including the AI-assisted ones
