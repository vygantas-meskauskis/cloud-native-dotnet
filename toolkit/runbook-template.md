# Runbook template

One runbook per alert. Link it from the alert description so that the person woken at 2 a.m. lands here in one click. Write it for someone who is tired and has never seen this service.

> Copy everything below the line into `docs/runbooks/<alert-name>.md`. Delete the hints in *italics*.

---

## Runbook: *alert name, exactly as it appears in the alert*

| | |
|---|---|
| **Service** | *e.g. Ledgerly worker* |
| **Owner** | *team and channel* |
| **Severity** | *Sev 1 (page now) / Sev 2 (page in working hours) / Sev 3 (ticket)* |
| **Last reviewed** | *date, by whom* |

### What this alert means

*One or two sentences in plain language. What is the user impact? Example: "Imports are not being processed. Users who upload a CSV see 'pending' forever. Nothing is lost: messages wait in the queue."*

### Is it real? (first 5 minutes)

1. *Open this dashboard: link.*
2. *Run this query: paste the KQL here.*
3. *Check the dependency status page: link.*
4. *If X is normal, it's a false alarm: resolve the alert and note it in the alert review.*

### Likely causes

| Signal | Likely cause | Go to |
|---|---|---|
| *Dependency failures to marketdata above 50%* | *Vendor outage* | *Fix A* |
| *Worker replicas restarting* | *Crash loop after deploy* | *Fix B* |
| *Messages in the dead-letter queue* | *Poison message* | *Fix C* |

### Fixes

#### Fix A: *dependency outage*

1. *Turn off the feature with the kill switch: exact flag name and where to change it.*
2. *Confirm users see the degraded experience, not errors.*
3. *Post in the status channel using the template below.*

#### Fix B: *bad deploy*

1. *Find the last good image tag: where to look.*
2. *Roll back: exact command or workflow to run.*
3. *Confirm the smoke test passes.*

#### Fix C: *poison message*

1. *How to read the dead-letter queue: tool and permissions.*
2. *How to decide: fix data and replay, or discard.*
3. *How to replay: exact steps.*

### Escalation

| When | Who | How |
|---|---|---|
| *Not mitigated in 30 minutes* | *Tech lead* | *Phone* |
| *Data loss suspected* | *Security and product* | *Incident channel* |

### Status message template

> *We are seeing [symptom] since [time]. [What users experience]. We are [action]. Next update in 30 minutes.*

### After the incident

- *Open a postmortem if it was Sev 1 or 2, or if it will happen again.*
- *Update this runbook with anything that was wrong or missing.*
