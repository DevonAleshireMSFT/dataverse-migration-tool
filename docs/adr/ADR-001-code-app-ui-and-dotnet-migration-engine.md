# ADR-001: Code App UI and .NET Migration Engine Boundary

- **Status:** Accepted
- **Date:** 2026-07-31
- **Proposed by:** Holden, Solution Architect

## Context

The Dataverse Migration Tool needs to move Dataverse data and solution components between Power Platform environments. Operators need an admin experience for selecting source and target environments, choosing components, validating readiness, starting runs, monitoring progress, and resuming failed jobs.

The presentation layer is a Power Platform Code App built with React and TypeScript. Code Apps run client-side in a browser tab. That is a good fit for an administrative control plane, but it is a poor fit for long-running migration work:

- browser execution is tied to tab lifetime, network stability, and user session lifetime;
- bulk Dataverse Web API calls require throttling, retry, checkpointing, and durable orchestration;
- resumable full and incremental jobs need durable job state and observability;
- enterprise and government tenants require supported identity flows, auditable operations, and cloud-specific endpoint handling.

## Decision

The Code App will be the admin UI and control plane. The actual migration engine will run server-side in .NET 9 and expose supported, secure APIs for validation, job creation, execution status, cancellation, and resume operations.

The initial backend host will be an ASP.NET Core Web API because it provides a straightforward composition root, dependency injection, health checks, API versioning options, observability hooks, and a clear boundary for the Code App. Azure Functions or Durable Functions remain valid deployment shapes for specific orchestration workloads, especially when timers, queues, or durable fan-out become necessary. The architectural boundary is the decision; the exact Azure hosting SKU can evolve without moving migration execution into the browser.

## Options Considered

### Option 1: All-client Code App using SDKs or connectors

- Benefits:
  - simplest deployment footprint;
  - fewer backend components to operate;
  - direct operator interaction from the UI.
- Drawbacks:
  - browser tabs are not reliable long-running workers;
  - difficult to make migrations resumable and durable;
  - risks exposing too much orchestration and credential-handling responsibility to the client;
  - limited centralized observability and throttling control;
  - poor fit for enterprise and GCC-High operational expectations.

### Option 2: Server-side .NET API / Functions migration engine

- Benefits:
  - durable job state, retries, throttling, and checkpointing can be centralized;
  - supports managed identity or other Microsoft-supported authentication patterns;
  - enables structured logging, metrics, tracing, audit records, and operational controls;
  - keeps Dataverse SDK usage and bulk Web API orchestration out of the UI;
  - aligns with clean architecture: UI depends on API contracts, infrastructure depends inward on application ports.
- Drawbacks:
  - requires deployment and operations for a backend host;
  - introduces API security, hosting, and environment configuration work;
  - needs clear tenancy and authorization design.

### Option 3: Hybrid client/server

- Benefits:
  - UI can perform lightweight validation and previews while server handles execution;
  - backend work can be introduced incrementally;
  - supports responsive operator workflows.
- Drawbacks:
  - boundary must be enforced to avoid migration logic leaking back into the client;
  - duplicated validation rules are possible if contracts are not managed carefully.

## Consequences

- The Code App must not perform bulk migration execution. It calls backend APIs to start, monitor, cancel, and resume jobs.
- The .NET backend owns Dataverse provider implementations, migration orchestration, validation orchestration, job persistence, operation logging, and observability.
- The Domain project remains independent of Dataverse SDKs, UI frameworks, and hosting concerns.
- The Application project defines ports and orchestration contracts. Infrastructure implements them. The API project is the composition root.
- Future hosting decisions can choose ASP.NET Core Web API, Azure Functions, Durable Functions, containers, or App Service as deployment details while preserving the UI/engine boundary.

## Security and Compliance Implications

- Use Microsoft-supported Dataverse APIs and SDKs only, including Dataverse Web API access through `Microsoft.PowerPlatform.Dataverse.Client` where appropriate.
- Keep secrets out of source code and client bundles. The backend should use configuration providers, managed identity where supported, federated credentials, or other approved tenant-specific auth mechanisms.
- The engine must account for public cloud, GCC, GCC High, and other sovereign cloud endpoint differences through configuration rather than hard-coded public-cloud assumptions.
- The backend must provide audit-friendly operation logs, correlation IDs, health checks, and observable job state.
- Authorization must protect migration operations as privileged administrative actions. The Code App is a control surface, not a trust boundary by itself.

