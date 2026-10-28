# Postmortem template

A postmortem is blameless: it asks how the system and the process allowed the failure, not who made the mistake. People who fear blame hide information, and then the same incident happens again.

> Copy everything below the line into `docs/postmortems/YYYY-MM-DD-short-title.md`. Write it within 5 working days of the incident while memories are fresh. Delete the hints in *italics*.

---

## Postmortem: *short title*

| | |
|---|---|
| **Date of incident** | |
| **Authors** | |
| **Status** | Draft / In review / Final |
| **Severity** | |
| **Duration** | *from first impact to full recovery* |
| **Error budget spent** | *e.g. 40% of the monthly budget for the valuation SLO* |

### Summary

*Three sentences: what happened, what users experienced, how it was fixed.*

### Impact

| | |
|---|---|
| Users affected | *number or share* |
| What they saw | *errors, slowness, wrong data, nothing* |
| Data lost or wrong | *yes/no, details* |
| Money or contractual impact | |

### Timeline

*All times in UTC. Include detection, decisions and dead ends, not only the fix.*

| Time (UTC) | What happened |
|---|---|
| | *First impact (found afterwards in the data)* |
| | *Alert fired / customer reported* |
| | *On-call acknowledged* |
| | *Hypothesis 1 tried, ruled out because...* |
| | *Mitigation applied* |
| | *Full recovery confirmed* |

### Detection

- *How did we find out? Alert, customer, colleague, luck?*
- *How long from first impact to detection? Could an SLO alert have caught it earlier?*

### Root causes and contributing factors

*Ask "why" until you reach something you can change in the system or process. There is usually more than one cause.*

1. *Trigger: the change or event that started it.*
2. *Why it caused impact: missing timeout, no idempotency, alert not actionable...*
3. *Why it took long to fix: runbook missing, rollback unclear, access problems...*

### What went well

- 

### What went badly

- 

### Where we got lucky

- 

### Action items

*Each item has an owner, a due date and an issue link. Prefer items that prevent a whole class of failures over items that fix only this one.*

| Action | Type | Owner | Due | Issue |
|---|---|---|---|---|
| | Prevent / Detect / Mitigate / Process | | | |

### Lessons for other teams

*One or two sentences worth sharing outside the team.*
