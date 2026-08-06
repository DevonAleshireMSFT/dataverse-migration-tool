# Definition of Ready and Definition of Done

This document defines the shared gates for when work is ready to start and when it is complete. It applies to product code, documentation, tests, spikes, and release-readiness work.

Related team standards:

- [Testing strategy and quality gates](testing-strategy.md)
- [Security review checklist](security/security-review-checklist.md)
- [CI/CD baseline strategy](ci-cd-strategy.md)
- [Architecture Decision Records](adr/README.md)

## Definition of Ready

Work is ready to start when all of the following are true:

- **Clear scope:** The issue states the user or team outcome, in-scope work, and out-of-scope boundaries clearly enough that an owner can execute without rediscovering intent.
- **Named owner:** A squad member, coding agent, or human owner is assigned and has the required domain authority for the work.
- **Dependencies identified:** Upstream issues, design decisions, credentials, environments, tools, docs, and cross-squad inputs are named. Any blockers are explicit.
- **Acceptance criteria are explicit:** The issue lists observable completion checks, including required docs, tests, review, and release-readiness evidence.
- **Risk is noted:** Architecture, security, data-loss, migration-correctness, sovereign-cloud, CI/CD, schedule, or unknown-unknown risks are documented with a proposed mitigation or spike.

### Work-type readiness nuance

| Work type | Additional readiness expectations |
| --- | --- |
| Features | User value, affected layers, expected behavior, non-goals, test expectations, and review owner are named. Architecture-impacting work identifies whether an ADR is needed. |
| Spikes | The question to answer, decision owner, time box, evaluation criteria, and expected follow-up artifact are named. |
| Docs | Audience, source of truth, files to update, and reviewers are identified. |
| Tests | Behavior under test, target layer, fixture/mock strategy, coverage expectation, and CI impact are identified. |

## Definition of Done

Work is done when all applicable gates below pass and the evidence is visible in the PR.

- **Acceptance criteria met:** Every issue acceptance criterion is satisfied or explicitly deferred with owner approval.
- **Tests updated and passing:** Tests are added or updated according to the [testing strategy](testing-strategy.md). Code changes must respect the coverage floor: **80% line coverage for combined backend test projects and 75% line coverage for the Code App once Vitest is wired**. Release-critical migration, validation, checkpoint/resume, and rollback logic should target **90%+ line coverage and meaningful branch coverage**.
- **CI is green:** Required build, test, lint, typecheck, format, and coverage gates from the [CI/CD baseline strategy](ci-cd-strategy.md) pass before merge.
- **Documentation updated:** User, operator, developer, architecture, security, or backlog docs are updated when behavior, workflow, decisions, configuration, risks, or ownership changes.
- **Security review complete:** Changes that touch authentication, authorization, Dataverse connectivity, configuration, secrets, job/state storage, logs, CI/CD, audit, or migration data handling satisfy the [security review checklist](security/security-review-checklist.md). Security-sensitive changes require Bobbie/security review before merge, and blocking conditions from that checklist must not be present.
- **Code and architecture review complete:** Code changes receive appropriate reviewer approval. Architecture-shaping changes follow the [ADR process](adr/README.md), including domain-owner review and Holden acceptance where required.
- **Operational readiness considered:** Migration correctness, rollback/resume behavior, observability, auditability, sovereign-cloud configuration, and release notes are addressed when relevant.

### Security DoD gates

For security-sensitive changes, the PR must explicitly confirm that:

- The threat model is updated for new surfaces, data flows, trust boundaries, identity flows, storage locations, log sinks, or CI/CD paths.
- Secret scanning is clean for source, docs, workflows, generated artifacts, and staged changes.
- Tokens, authorization headers, client secrets, certificates, passwords, connection strings, raw PII, and raw migration payloads are never logged, persisted in job/browser state, or committed.
- Least-privilege Dataverse scopes and roles are documented and verified for source and target environments.
- Source/target tenant, cloud, authority host, Dataverse resource, scopes, and sovereign-cloud endpoint selection remain configurable and validated.
- CI/CD changes use least-privilege permissions, remain secretless by default, and protect any future deployment identity behind environment approvals.

## Work-type completion nuance

| Work type | Done means |
| --- | --- |
| Features | Behavior is implemented, tested at the right layer, documented, CI-green, security-reviewed when applicable, and architecture-reviewed or ADR-backed when boundaries or contracts change. |
| Spikes | The spike produces a decision-ready plan, recommended path, trade-offs, risks, and a follow-up backlog of issues or ADRs. A spike is not done with research notes alone. |
| Docs | The document is accurate, linked from the appropriate source of truth when needed, cross-references related standards, and has no broken relative links. |
| Tests | Tests are deterministic, hermetic by default, aligned to the test pyramid, avoid live Dataverse dependencies in default CI, and improve or preserve the relevant coverage floor. |
