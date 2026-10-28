# SLO worksheet

Fill this in with your product manager and your SRE, not alone. The goal is a few numbers everybody agrees on: what "healthy" means, and when reliability work beats feature work.

> The Ledgerly values below are **illustrative examples**. Replace them with your own.

## Step 1: List the user journeys that matter

Pick 3 to 5. If a journey breaks and nobody notices for a day, it doesn't belong here.

| # | Journey | Who notices when it breaks | Ledgerly example |
|---|---|---|---|
| 1 | | | See my portfolio valuation |
| 2 | | | Record a trade |
| 3 | | | Import a broker CSV |
| 4 | | | Get a price alert |

## Step 2: Choose an SLI for each journey

An SLI (service level indicator) is a ratio: **good events / valid events**, measured where the user would notice.

| Journey | SLI type | Good event | Valid event | Where it's measured | Ledgerly example |
|---|---|---|---|---|---|
| | Availability | | | | Valuation requests that return non-5xx |
| | Latency | | | | Valuation requests faster than 500 ms |
| | Freshness | | | | Imports finished within 5 minutes of upload |
| | Correctness | | | | Valuations with `isComplete = true` |

Rules of thumb:

- Measure at the edge (API requests), not on internal components.
- Exclude events the user caused (4xx validation errors) from "valid".
- Prefer one good SLI over five mediocre ones.

## Step 3: Set the SLO

An SLO (service level objective) is a target for the SLI over a window. Start with what you achieve today, minus a little margin, not with "five nines".

| SLI | SLO target | Window | Ledgerly example |
|---|---|---|---|
| | | 28 days | 99.5% of valuation requests succeed |
| | | 28 days | 95% of valuation requests under 500 ms |
| | | 28 days | 99% of imports finish within 5 minutes |

## Step 4: Calculate the error budget

Error budget = 1 − SLO. It is the amount of unreliability you are allowed to spend on deploys, experiments and bad luck.

| SLO | Error budget | Over 28 days, that is |
|---|---|---|
| 99% | 1% | about 6.7 hours of total failure, or 1 in 100 requests |
| 99.5% | 0.5% | about 3.4 hours, or 1 in 200 requests |
| 99.9% | 0.1% | about 40 minutes, or 1 in 1,000 requests |

Your numbers:

| SLO | Valid events per 28 days (estimate) | Allowed bad events |
|---|---|---|
| | | |

## Step 5: Agree what happens when the budget runs out

Write this down before you need it. Example policy:

| Budget left | What the team does |
|---|---|
| More than 50% | Ship normally. Experiments are fine. |
| 25% to 50% | Review the top error sources in the weekly meeting. |
| Less than 25% | Reliability work goes first in the next sprint. Risky deploys need a second reviewer. |
| Exhausted | Feature freeze for the service until the budget recovers, except fixes. |

Signed off by (product, engineering, SRE): ____________________

## Step 6: Alert on burn rate, not on single errors

Burn rate = how fast you are spending the budget compared to "evenly over the window". A burn rate of 1 uses exactly the whole budget in 28 days.

| Alert | Burn rate | Long window (short window) | Action |
|---|---|---|---|
| Fast burn | 14.4 | 1 hour (5 minutes) | Page on-call |
| Medium burn | 6 | 6 hours (30 minutes) | Page on-call |
| Slow burn | 1 | 3 days (6 hours) | Ticket for the next working day |

These values come from the multiwindow, multi-burn-rate example in the [Google SRE Workbook](https://sre.google/workbook/implementing-slos/), written for a 30-day window. The alert fires only when both the long and the short window exceed the rate, so it stops quickly after recovery. Tune them for your traffic: low-traffic services need longer windows, or a single failed request will page someone.

## Step 7: Wire it up

| Task | Done |
|---|---|
| SLI query written and tested (see the KQL queries file, query 11) | |
| Workbook tile showing SLI vs SLO for the window | |
| Fast-burn alert with action group and runbook link | |
| Slow-burn alert as a ticket or email | |
| Monthly SLO review in the team calendar | |

## Review questions (every month)

1. Did we meet the SLO? If we beat it by a lot, is it too loose?
2. What spent the budget: deploys, a dependency, load, or bugs?
3. Did any outage happen that the SLI didn't see? Then the SLI is wrong.
4. Did any alert fire that nobody needed to act on? Then the alert is wrong.
