# Product Vision and Architecture Overview

## Vision

Dataverse Migration Tool helps administrators move Dataverse data and solution components between Power Platform environments with confidence. It is built for secure, repeatable migrations that can be planned, validated, executed, monitored, paused, resumed, and audited.

The product supports two migration paths:

- **Full migration** — move an agreed scope of data and solution components from a source environment to a target environment.
- **Incremental migration** — detect and apply changes after a prior run so teams can rehearse, cut over, or keep environments aligned without starting from zero.

Every run should produce validation reports that show readiness issues, skipped items, failures, warnings, and post-run reconciliation status. Rollback guidance is part of the operator workflow: the tool should identify what changed, what can be safely reversed, and where rollback requires tenant-specific backup, restore, or manual remediation steps.

## Audience

- Power Platform and Dataverse administrators planning controlled migrations.
- Enterprise delivery teams rehearsing and executing environment cutovers.
- Government and regulated teams that require supported APIs, auditable operations, and cloud-aware configuration.

## Goals

- Keep migration execution durable and resumable outside the browser.
- Make validation and reporting first-class, not afterthoughts.
- Separate business rules from Dataverse, hosting, and UI infrastructure.
- Support public cloud, GCC, and GCC High readiness through configuration, identity, logging, and deployment choices.
- Use only Microsoft-supported Dataverse, Power Platform, and Azure APIs and tooling; do not depend on undocumented or internal endpoints.

## Non-goals

- Replacing Dataverse backup/restore, ALM, or tenant governance processes.
- Building an unsupported data extraction path around private service endpoints.
- Running long-lived bulk migration work inside the Code App browser session.

## Top-level architecture

The architecture follows the clean/onion boundary accepted in [ADR-001](adr/ADR-001-code-app-ui-and-dotnet-migration-engine.md) and implemented in the [.NET backend](../src/backend/README.md):

```text
Presentation / Code App -> Application -> Domain
Presentation / Code App -> API host -> Infrastructure -> Application -> Domain
```

### Presentation / Code App

The Power Platform Code App is the operator control plane. It lets users select environments and scope, start validation, create migration jobs, monitor progress, resume failed jobs, and review reports. It does not own bulk migration execution or Dataverse provider logic.

### Application

The Application layer defines use cases, ports, orchestration contracts, and result models for migration, validation, configuration, job state, and logging. It coordinates workflows while depending only on the Domain layer.

### Domain

The Domain layer contains migration concepts, value objects, enums, and business rules that are independent of Dataverse SDKs, ASP.NET Core, React, Fluent UI, storage, or cloud hosting.

### Infrastructure

The Infrastructure layer implements Application ports for Dataverse access, durable job state, validation execution, configuration, and operation logging. It is the only layer that should know about external SDKs, storage providers, cloud endpoints, and platform-specific adapters.

## Named subsystems

- **Dataverse Provider** — Microsoft-supported Dataverse API/SDK adapter for source and target environment reads, writes, metadata, throttling, retries, and cloud endpoint selection.
- **Migration Engine** — server-side orchestration for full and incremental jobs, batching, checkpointing, resume, cancellation, operation logs, and reconciliation.
- **Validation Engine** — pre-run and post-run checks for schema, dependencies, permissions, data readiness, row counts, warnings, failures, and report generation.
- **Configuration Provider** — environment, tenant, identity, endpoint, feature flag, and policy configuration without hard-coded public-cloud assumptions.

## Enterprise and government readiness

The tool is designed to become enterprise and government ready by keeping privileged work server-side, protecting secrets from client bundles and source control, emitting audit-friendly logs and correlation IDs, and selecting public cloud, GCC, or GCC High endpoints through supported configuration. Deployment details may evolve, but the principle does not: migrations must rely on documented, supported Microsoft APIs and authentication patterns only.
