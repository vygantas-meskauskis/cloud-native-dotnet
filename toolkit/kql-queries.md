# KQL queries

Starting-point queries for a Ledgerly-style system in Application Insights (workspace-based). Adapt the names, thresholds and time ranges, then save the good ones as workbook tiles or log search alerts.

> 🔧 These use the **Log Analytics table names** (`AppRequests`, `AppDependencies`, `AppTraces`, `AppExceptions`, `AppMetrics`) and their columns (`TimeGenerated`, `AppRoleName`, `DurationMs`). If you run them from the Application Insights **Logs** blade, the classic names also work there: `requests`, `dependencies`, `traces`, `exceptions`, `customMetrics`, with `timestamp` and `cloud_RoleName`. Check the column names in your workspace schema before you build alerts on them.

`AppRoleName` is the OpenTelemetry `service.name`. In the sample it comes from `OTEL_SERVICE_NAME`: `api`, `worker` and `marketdata`.

## 1. Failure rate per service (5-minute buckets)

```kusto
AppRequests
| where TimeGenerated > ago(6h)
| summarize total = count(), failed = countif(Success == false) by AppRoleName, bin(TimeGenerated, 5m)
| extend failureRatePct = round(100.0 * failed / total, 2)
| order by TimeGenerated desc
```

Use it for: the "is it broken right now?" tile. 🔧 How `Success` treats 4xx responses depends on the SDK and exporter, so check a few rows first. If needed, define failure yourself with `toint(ResultCode) >= 500`: a user's validation error is not your outage.

## 2. p50, p95 and p99 latency per endpoint

```kusto
AppRequests
| where TimeGenerated > ago(24h) and AppRoleName == "api"
| summarize requests = count(),
            p50 = percentile(DurationMs, 50),
            p95 = percentile(DurationMs, 95),
            p99 = percentile(DurationMs, 99) by Name
| order by p95 desc
```

Use it for: finding the slow endpoints. `Name` is the route template (for example `GET /api/portfolios/{id:guid}/valuation`), so IDs don't explode the result.

## 3. Calls to the market-data vendor: failures and latency

```kusto
AppDependencies
| where TimeGenerated > ago(6h)
| where AppRoleName == "api" and Target has "marketdata"
| summarize calls = count(),
            failed = countif(Success == false),
            p95 = percentile(DurationMs, 95) by bin(TimeGenerated, 5m)
| extend failureRatePct = round(100.0 * failed / calls, 2)
| render timechart
```

Use it for: telling "our API is slow" apart from "the vendor is slow". Retries from the resilience handler appear as separate dependency calls under the same operation.

## 4. Partial valuations (graceful degradation in action)

```kusto
AppTraces
| where TimeGenerated > ago(24h)
| where Message has "returning partial valuation"
| extend Symbol = tostring(Properties.Symbol), Reason = tostring(Properties.Reason)
| summarize count() by Symbol, Reason, bin(TimeGenerated, 15m)
| order by TimeGenerated desc
```

Use it for: seeing what users experienced while the vendor was failing. The message comes from the `[LoggerMessage]` in `ValuationService`. Template placeholders become properties, so you can group by them. 🔧 The exact property names depend on the exporter. Run `AppTraces | take 10` and look at `Properties` first.

## 5. Import throughput and outcomes (custom metric)

```kusto
AppMetrics
| where TimeGenerated > ago(24h)
| where Name == "ledgerly.imports.processed"
| extend outcome = tostring(Properties.outcome)
| summarize imports = sum(Sum) by outcome, bin(TimeGenerated, 1h)
| render columnchart
```

Use it for: Elena's question "how many imports ran today, and how many had errors?" The counter is tagged with `outcome` (`completed` or `completed_with_errors`). Metrics arrive pre-aggregated, so you `sum(Sum)` instead of `count()`.

## 6. Import duration (custom histogram)

```kusto
AppMetrics
| where TimeGenerated > ago(24h)
| where Name == "ledgerly.imports.duration"
| summarize totalSeconds = sum(Sum), imports = sum(ItemCount), slowest = max(Max) by bin(TimeGenerated, 1h)
| extend avgSeconds = round(totalSeconds / imports, 2)
```

Use it for: spotting imports getting slower as files grow. Histograms are exported as aggregates (sum, count, min, max), so you get an average and a maximum, not exact percentiles.

## 7. Messages being retried or dead-lettered

```kusto
AppTraces
| where TimeGenerated > ago(24h) and AppRoleName == "worker"
| where Message has "abandoning for retry" or Message has "already processed"
| extend kind = iff(Message has "abandoning", "retry", "duplicate")
| summarize count() by kind, bin(TimeGenerated, 15m)
| render timechart
```

Use it for: an early warning before messages reach the dead-letter queue. Repeated retries for the same import mean a poison message. The dead-letter count itself is a Service Bus metric on the queue: alert on it with a metric alert, not with KQL.

## 8. Exceptions grouped by problem

```kusto
AppExceptions
| where TimeGenerated > ago(24h)
| summarize count(), firstSeen = min(TimeGenerated), lastSeen = max(TimeGenerated),
            sample = take_any(OuterMessage) by ProblemId, AppRoleName
| order by count_ desc
```

Use it for: the morning check. A new `ProblemId` that appeared after the last deploy is your first suspect.

## 9. Follow one request end to end

```kusto
let op = "<paste an OperationId here>";
union AppRequests, AppDependencies, AppTraces, AppExceptions
| where OperationId == op
| project TimeGenerated, itemType = Type, AppRoleName, Name, Message, DurationMs, Success
| order by TimeGenerated asc
```

Use it for: the bug report with a timestamp. The `OperationId` is the W3C trace ID, so this includes the worker's processing of an import that the API accepted, because the sample carries trace context through the outbox and Service Bus. The portal's end-to-end transaction view shows the same data as a timeline.

## 10. Replica starts (scale-outs, restarts, cold starts)

```kusto
AppTraces
| where TimeGenerated > ago(24h)
| where Message has "Application started"
| summarize starts = count() by AppRoleName, bin(TimeGenerated, 1h)
| render columnchart
```

Use it for: correlating latency spikes with replicas starting. Many starts for `api` with `minReplicas` 0 means users are hitting cold starts. Unexpected starts for `worker` (min 1) mean crashes or failed liveness probes.

## 11. SLI: share of good valuation requests (for the SLO worksheet)

```kusto
AppRequests
| where TimeGenerated > ago(28d) and AppRoleName == "api"
| where Name has "valuation"
| summarize total = count(), good = countif(Success == true and DurationMs < 500)
| extend sliPct = round(100.0 * good / total, 3)
```

Use it for: the monthly SLO report. The 500 ms threshold is illustrative; use the one from your SLO worksheet.

## Turning a query into an alert

| Step | What to decide |
|---|---|
| 1 | Is it a symptom users feel (failure rate, latency) or a cause (CPU)? Alert on symptoms. |
| 2 | Window and frequency: a 5-minute window evaluated every 5 minutes is a sane start. |
| 3 | Threshold: start from the SLO, not from a gut feeling. |
| 4 | Action group: who gets paged, who only gets an email. |
| 5 | Runbook: put the link in the alert description (see the runbook template). |

💰 Log ingestion is often the surprise cost of observability, and alert rules have their own charges (check the Azure Monitor pricing page). Prefer a few well-chosen alerts over many noisy ones, and set sampling and a daily cap on purpose.
