# Backlog Taxonomy and Roadmap

This backlog translates the [Product Vision and Architecture Overview](product-vision.md) into release-sized work while preserving the architecture decision workflow described in [Architecture Decision Records](adr/README.md). It is intentionally maintained at epic, feature, spike, chore, and docs granularity until an epic is ready for implementation planning.

## Backlog taxonomy

| Work item type | Label convention | Definition | Done when |
| --- | --- | --- | --- |
| Epic | `type:epic` | A durable product or architecture outcome that groups multiple features, spikes, chores, or docs items. Epics define the destination, not the implementation checklist. | The epic's release-scope children are complete or intentionally deferred. |
| Feature | `type:feature` | User- or system-visible capability that changes product behavior, architecture contracts, or operational workflows. | Code, tests, docs, and acceptance criteria for the feature are complete. |
| Spike | `type:spike` | Time-boxed research used when supported APIs, compliance constraints, migration mechanics, or design trade-offs are not yet known. | Findings, recommendation, risks, and follow-up backlog items are documented. |
| Chore | `type:chore` | Repository, process, infrastructure, or governance work that enables delivery but is not itself a product feature. | The enabling artifact or automation is in place and documented. |
| Docs | `type:docs` | Product, architecture, governance, contributor, runbook, or reference documentation. | The intended audience can use the published document without private context. |

Labels in current use:

- `release:*` assigns the target release: `release:v0.4.0`, `release:v0.5.0`, `release:v0.6.0`, or `release:v1.0.0`.
- `priority:*` orders work within and across releases: `priority:p0`, `priority:p1`, and `priority:p2`.
- `go:*` records readiness: `go:yes` for ready work and `go:needs-research` when a spike or discovery step must resolve uncertainty first.
- `squad:*` routes ownership to the accountable squad member or worker pool, for example `squad:holden`, `squad:naomi`, `squad:amos`, `squad:alex`, `squad:bobbie`, `squad:prax`, `squad:drummer`, `squad:monica`, or `squad:copilot`.
- Multiple `type:*` labels are allowed when an item legitimately spans categories, such as `type:docs` plus `type:chore`.

## Epic structure

The nine epics below are the authoritative structure established by Holden's initial backlog decision.

| Epic issue | Epic | Primary owner label | Release label | Purpose |
| --- | --- | --- | --- | --- |
| #1 | Project Foundations & Governance | `squad:holden` | `release:v0.4.0` | Establish product vision, architecture boundaries, repository shape, backlog taxonomy, readiness definitions, and contributor conventions. |
| #2 | Dataverse Connectivity & Environment Intelligence | `squad:naomi` | `release:v0.5.0` | Define and implement supported Dataverse connectivity, metadata discovery, environment comparison, and readiness intelligence. |
| #3 | Migration Engine & Data Movement | `squad:amos` | `release:v0.5.0` | Deliver full migration execution, then advanced delta, checkpoint, resume, idempotency, and recovery behavior. |
| #4 | Solution Component Migration | `squad:naomi` | `release:v0.6.0` | Research and orchestrate supported solution-component movement through approved tooling and APIs. |
| #5 | Validation, Testing & Quality Gates | `squad:prax` | `release:v0.5.0` | Make validation contracts, report models, test strategy, and release quality gates first-class. |
| #6 | Code App UI & Operator Workflow | `squad:alex` | `release:v0.5.0` | Provide the operator control plane for selecting environments, configuring scope, launching work, and monitoring results. |
| #7 | Security, Identity & Government Readiness | `squad:bobbie` | `release:v1.0.0` | Harden identity, tenant boundaries, secrets, threat model, and GCC/GCC High readiness. |
| #8 | DevOps, Release & Documentation Operations | `squad:drummer` | `release:v1.0.0` | Own CI/CD, release operations, documentation operations, runbooks, and reference publishing. |
| #9 | Extensibility, Configuration & Observability | `squad:holden` | `release:v1.0.0` | Govern configuration, environment profiles, observability, plugin extensibility, and trust boundaries. |

## Roadmap by release

### v0.4.0 foundations

Foundational `priority:p0` work is complete and closed: #10, #11, #12, #18, #30, #39, #40, #43, and #48. The risk register (#32) is also done. The remaining v0.4.0 work is documentation and governance polish: #13, #14, and #16.

| Issue | State | Type labels | Priority | Go | Squad | Summary |
| --- | --- | --- | --- | --- | --- | --- |
| #1 | Open | `type:epic` | `priority:p0` | `go:yes` | `squad:holden` | Project Foundations & Governance epic. |
| #10 | Closed | `type:docs` | `priority:p0` | `go:yes` | `squad:holden` | Product Vision and solution architecture overview. |
| #11 | Closed | `type:spike`, `type:docs` | `priority:p0` | `go:needs-research` | `squad:holden` | ADR-0001 and ADR process for clean architecture boundaries. |
| #12 | Closed | `type:feature`, `type:chore` | `priority:p0` | `go:yes` | `squad:holden` | Repository structure, solution skeleton, and composition root. |
| #13 | Open | `type:docs`, `type:chore` | `priority:p1` | `go:yes` | `squad:holden`, `squad:copilot` | Backlog taxonomy, roadmap, and user-story deferral plan. |
| #14 | Open | `type:docs`, `type:chore` | `priority:p1` | `go:yes` | `squad:holden`, `squad:copilot` | Definition of Ready and Definition of Done. |
| #16 | Open | `type:docs`, `type:chore` | `priority:p1` | `go:yes` | `squad:holden`, `squad:copilot` | Coding standards and contribution conventions. |
| #18 | Closed | `type:feature` | `priority:p0` | `go:yes` | `squad:naomi` | Dataverse Provider connectivity contracts and auth handoff. |
| #30 | Closed | `type:docs`, `type:chore` | `priority:p0` | `go:yes` | `squad:prax` | Testing strategy and quality gates. |
| #32 | Closed | `type:spike`, `type:docs` | `priority:p1` | `go:needs-research` | `squad:prax`, `squad:copilot` | Risk register and mitigation backlog. |
| #39 | Closed | `type:spike`, `type:docs` | `priority:p0` | `go:needs-research` | `squad:bobbie` | Security strategy and threat model. |
| #40 | Closed | `type:feature` | `priority:p0` | `go:yes` | `squad:bobbie` | MSAL/Entra auth, tenant boundary, and secret-handling skeleton. |
| #43 | Closed | `type:feature`, `type:docs` | `priority:p0` | `go:yes` | `squad:drummer`, `squad:copilot` | CI/CD baseline strategy and GitHub Actions scaffold. |
| #48 | Closed | `type:feature` | `priority:p0` | `go:yes` | `squad:holden` | Configuration Provider schema and environment profiles. |

### v0.5.0 core

Core release work turns the foundations into the first usable migration path: supported connectivity, full data movement, validation contracts, and the Code App operator shell.

| Issue | State | Type labels | Priority | Go | Squad | Summary |
| --- | --- | --- | --- | --- | --- | --- |
| #2 | Open | `type:epic` | `priority:p0` | `go:yes` | `squad:naomi` | Dataverse Connectivity & Environment Intelligence epic. |
| #3 | Open | `type:epic` | `priority:p0` | `go:yes` | `squad:amos` | Migration Engine & Data Movement epic. |
| #5 | Open | `type:epic` | `priority:p1` | `go:yes` | `squad:prax` | Validation, Testing & Quality Gates epic. |
| #6 | Open | `type:epic` | `priority:p1` | `go:yes` | `squad:alex` | Code App UI & Operator Workflow epic. |
| #20 | Open | `type:feature` | `priority:p0` | `go:yes` | `squad:naomi` | Metadata discovery and caching. |
| #21 | Open | `type:feature` | `priority:p1` | `go:yes` | `squad:naomi` | Environment compare and migration readiness report. |
| #23 | Open | `type:feature` | `priority:p0` | `go:yes` | `squad:amos` | Full data migration execution. |
| #31 | Open | `type:feature` | `priority:p1` | `go:yes` | `squad:prax` | Validation Engine v1 contracts and report model. |
| #35 | Open | `type:feature` | `priority:p1` | `go:yes` | `squad:alex`, `squad:copilot` | Code App shell, navigation, and Fluent UI foundation. |

### v0.6.0 advanced

Advanced release work expands beyond the first migration path into deltas, resumability, rollback guidance, and supported solution-component migration.

| Issue | State | Type labels | Priority | Go | Squad | Summary |
| --- | --- | --- | --- | --- | --- | --- |
| #4 | Open | `type:epic` | `priority:p1` | `go:needs-research` | `squad:naomi` | Solution Component Migration epic. |
| #24 | Open | `type:spike` | `priority:p1` | `go:needs-research` | `squad:amos` | Incremental and delta migration strategy. |
| #25 | Open | `type:feature` | `priority:p1` | `go:yes` | `squad:amos` | Checkpoint, resume, and idempotency support. |
| #26 | Open | `type:feature` | `priority:p1` | `go:yes` | `squad:amos` | Rollback guidance and recovery planning. |
| #27 | Open | `type:spike` | `priority:p1` | `go:needs-research` | `squad:naomi` | Supported solution-component migration surface. |
| #28 | Open | `type:feature` | `priority:p2` | `go:needs-research` | `squad:naomi` | Solution export/import orchestration via supported tooling. |

### v1.0.0 hardening, compliance, and docs

The v1.0.0 release hardens the product for enterprise and government expectations, completes operations documentation, and defines extension trust boundaries.

| Issue | State | Type labels | Priority | Go | Squad | Summary |
| --- | --- | --- | --- | --- | --- | --- |
| #7 | Open | `type:epic` | `priority:p0` | `go:yes` | `squad:bobbie` | Security, Identity & Government Readiness epic. |
| #8 | Open | `type:epic` | `priority:p1` | `go:yes` | `squad:drummer` | DevOps, Release & Documentation Operations epic. |
| #9 | Open | `type:epic` | `priority:p1` | `go:yes` | `squad:holden` | Extensibility, Configuration & Observability epic. |
| #41 | Open | `type:spike` | `priority:p1` | `go:needs-research` | `squad:bobbie` | GCC/High compliance and cloud endpoint readiness. |
| #47 | Open | `type:docs` | `priority:p1` | `go:yes` | `squad:monica`, `squad:copilot` | Full documentation, runbooks, and reference docs. |
| #51 | Open | `type:spike` | `priority:p2` | `go:needs-research` | `squad:holden` | Plugin extensibility model and trust boundaries. |

## User-story deferral rule

Detailed user stories are deliberately deferred. The backlog should not be decomposed into full story trees up front. Instead, stories are created just-in-time per epic when the owning squad member is ready to plan implementation, after relevant spikes and ADRs have resolved material uncertainty. This keeps the backlog readable, avoids premature commitments, and lets stories reflect the latest product, architecture, compliance, and supported-API decisions.
