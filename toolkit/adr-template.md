# ADR template

An architecture decision record (ADR) captures one significant decision: the context, the options, the choice and its consequences. Six months later, nobody remembers why you picked Container Apps over App Service. The ADR does.

Write one when a decision is hard to reverse, affects more than one team, or has been argued about twice.

> Copy everything below the line into `docs/adr/NNNN-short-title.md` (number them in order and never reuse a number). Delete the hints in *italics*. To change a decision, write a new ADR that supersedes the old one; don't edit history.

---

## ADR NNNN: *decision title, as a short statement*

| | |
|---|---|
| **Status** | Proposed / Accepted / Superseded by ADR NNNN / Deprecated |
| **Date** | |
| **Deciders** | |
| **Consulted** | |

### Context

*What problem are we solving? What constraints apply (team skills, budget, deadlines, compliance)? What forces are in tension? Keep it factual.*

### Options considered

| Option | Pros | Cons |
|---|---|---|
| *A* | | |
| *B* | | |
| *C: do nothing* | | |

### Decision

*We will ... because ...*

### Consequences

- **Good:** *what gets easier*
- **Bad:** *what gets harder, and the debt we accept*
- **Follow-up:** *work this creates, with issue links*

### When to revisit

*The signal that would make us reconsider, e.g. "if we need more than 30 services" or "if the monthly bill passes X".*

---

## Example (illustrative): ADR 0003, host the services on Azure Container Apps

| | |
|---|---|
| **Status** | Accepted |
| **Deciders** | Maya Okafor, Tomás Varga |
| **Consulted** | Priya Raman, Jonas Berg, Elena Rossi |

**Context.** Four small services (API, worker, market data, migrations), a team of six, a beta in 12 weeks. Nobody on the team runs Kubernetes today. The worker must scale on queue length. Traffic during the beta is low and spiky.

**Options considered.**

| Option | Pros | Cons |
|---|---|---|
| App Service | Closest to the IIS model, mature | Queue-based scaling of the worker is harder, no scale to zero |
| Container Apps | Scale to zero, KEDA rules for Service Bus, jobs for migrations, no cluster to run | Cold starts at zero replicas, fewer knobs than Kubernetes |
| AKS | Full control, huge ecosystem | We'd spend the beta learning to operate a cluster |

**Decision.** We will run all services on Azure Container Apps in one environment, because it gives us queue-based scaling and run-to-completion jobs without operating a cluster.

**Consequences.** Good: low idle cost, one deploy model for every service. Bad: cold starts on the API unless we keep one replica (a cost decision for Elena). Follow-up: the migrations job (chapter 12), scale rules (chapter 18).

**When to revisit.** If we need service mesh features, or more than about 20 services.
