# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for the Dataverse Migration Tool. ADRs live in `docs\adr\` and record decisions that shape architecture, quality attributes, cross-cutting contracts, or team workflow.

For the current product direction and top-level layer map, see the [Product Vision and Architecture Overview](../product-vision.md).

## Accepted ADRs

- [ADR-001: Code App UI and .NET Migration Engine Boundary](ADR-001-code-app-ui-and-dotnet-migration-engine.md)
- [ADR-0002: Clean Architecture Boundaries](ADR-0002-clean-architecture-boundaries.md)

## Format

Use [adr-template.md](adr-template.md). Every ADR must include:

- status;
- date;
- proposer;
- decision authority;
- context;
- decision;
- alternatives considered;
- consequences.

## Status lifecycle

ADRs move through this lifecycle:

```text
Proposed -> Accepted -> Superseded
                    -> Deprecated
```

- **Proposed:** a decision candidate under review.
- **Accepted:** the decision is authoritative for future work.
- **Superseded:** a newer ADR replaces the decision. Link to the superseding ADR.
- **Deprecated:** the decision is no longer recommended, but no replacement is yet accepted.

Accepted ADRs are immutable. Do not edit an accepted ADR to change a decision. Correct typos only when they do not alter meaning. If the architecture changes, create a new ADR and mark the older decision as superseded from the new ADR.

## Ownership and review workflow

- **Accepting authority:** Holden, Lead / Solution Architect.
- **Review contributors:** the owning squad member for the affected domain should review proposed ADRs before acceptance. Examples: Naomi for Dataverse provider mechanics, Amos for migration engine internals, Alex for Code App presentation, Bobbie for security and identity.
- **Owner escalation:** scope, licensing, public OSS posture, or government-readiness commitments escalate to Devon Aleshire.

## How to propose an ADR

1. Copy `docs\adr\adr-template.md` to a new file in `docs\adr\`.
2. Use the next sequential ADR number and a short kebab-case title: `ADR-0003-short-title.md`.
3. Set `Status: Proposed`, include the current date, name the proposer, and set `Decision authority: Holden, Lead / Solution Architect`.
4. Describe the context, decision, alternatives considered, and consequences in terms of forces and trade-offs.
5. Ask Holden for acceptance after domain-owner review is complete.
6. Once accepted, set `Status: Accepted`, fill `Accepted by`, and treat the ADR as immutable.

`ADR-001` is retained intact as an already accepted record. New ADR filenames use four-digit zero-padded numbering beginning with `ADR-0002`.

## Supersession rules

- A new ADR may supersede one or more accepted ADRs.
- The new ADR must state which ADRs it supersedes and why.
- Do not rewrite the old decision body. If a status note is needed, only add a brief supersession pointer that does not change the historical decision.
- Code and documentation should follow the newest accepted ADR when records conflict.

## Follow-up ADR backlog

Decision-ready future ADRs to write:

- Persistence and state store for jobs, checkpoints, operation logs, and resume behavior.
- Migration ordering and dependency resolution for data and solution components.
- Validation model for pre-run checks, post-run reconciliation, severity, and reports.
- Identity and authorization boundary for supported auth flows, tenant consent, RBAC, and privileged operations.
