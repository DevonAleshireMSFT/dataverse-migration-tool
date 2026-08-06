# ADR-0002: Clean Architecture Boundaries

- **Status:** Accepted
- **Date:** 2026-08-06
- **Proposed by:** Holden, Lead / Solution Architect
- **Decision authority:** Holden, Lead / Solution Architect
- **Accepted by:** Holden, Lead / Solution Architect
- **Supersedes:** None
- **Superseded by:** None

## Context

The Dataverse Migration Tool needs a durable migration engine, an operator-facing Code App, Dataverse connectivity, validation, state, logging, and cloud-aware configuration. The [Product Vision and Architecture Overview](../product-vision.md) requires business rules to stay separate from Dataverse, hosting, and UI infrastructure, and [ADR-001](ADR-001-code-app-ui-and-dotnet-migration-engine.md) places bulk migration execution in a server-side .NET backend instead of the browser.

The current [.NET backend](../../src/backend/README.md) already reflects this direction with separate Domain, Application, Infrastructure, API, and test projects. This ADR makes those boundaries authoritative so future work does not blur provider, orchestration, or presentation responsibilities.

## Decision

Use clean/onion architecture boundaries for the backend and preserve these dependencies:

```text
Presentation / Code App -> API host -> Application -> Domain
API host -> Infrastructure -> Application -> Domain
```

The authoritative layer responsibilities are:

- **Domain** owns migration concepts, entities, value objects, enums, and business rules. It has zero references to the Dataverse SDK, ASP.NET Core, UI frameworks, storage providers, cloud hosts, or infrastructure packages.
- **Application** owns use cases, orchestration contracts, ports, request/result models, and workflow coordination. It depends inward on Domain only.
- **Infrastructure** owns adapters for Dataverse access, identity/token acquisition implementations, configuration providers, durable state stores, validation adapters, logging sinks, and other external systems. It implements Application ports and depends inward on Application and Domain.
- **Presentation** owns operator interaction. The Code App is the control plane, and the ASP.NET Core API host is the backend HTTP surface and composition root. Presentation code may call Application contracts through the API but must not own migration execution or Dataverse provider logic.

Dependencies point inward. Outer layers may reference inner layers; inner layers must not reference outer layers. Domain purity is a merge-blocking architectural rule.

Existing implementation evidence:

- [`src\backend\DataverseMigrationTool.Domain`](../../src/backend/DataverseMigrationTool.Domain) has no external package references.
- [`src\backend\DataverseMigrationTool.Application`](../../src/backend/DataverseMigrationTool.Application) references Domain and defines ports such as [`IDataverseProvider`](../../src/backend/DataverseMigrationTool.Application/Ports/IDataverseProvider.cs) and [`IDataverseTokenProvider`](../../src/backend/DataverseMigrationTool.Application/Ports/IDataverseTokenProvider.cs).
- [`src\backend\DataverseMigrationTool.Infrastructure`](../../src/backend/DataverseMigrationTool.Infrastructure) references Application and Domain and contains Dataverse provider/token implementations.
- [`src\backend\DataverseMigrationTool.Api\Program.cs`](../../src/backend/DataverseMigrationTool.Api/Program.cs) is the composition root and wires configuration plus Infrastructure registrations.

## Alternatives Considered

### Alternative 1: Transaction-script service layer

- Benefits:
  - Fastest path for early prototypes.
  - Fewer projects and abstractions to explain.
- Drawbacks:
  - Dataverse SDK calls, validation rules, state handling, and orchestration would tend to mix in the same services.
  - Harder to test migration rules without live provider dependencies.
  - Encourages business rules to depend on infrastructure details.

### Alternative 2: Anemic three-tier layering

- Benefits:
  - Familiar UI/API/data split for many contributors.
  - Simple mental model for CRUD-oriented applications.
- Drawbacks:
  - Migration behavior is workflow-heavy, not simple CRUD.
  - A data-access-centric design would make Dataverse and persistence concerns too influential over core rules.
  - Domain concepts would likely become DTOs with rules scattered across services.

### Alternative 3: Vertical slices as the primary architecture

- Benefits:
  - Feature folders can keep related request handlers, validation, and UI work discoverable.
  - Useful inside Application or Presentation once boundaries are established.
- Drawbacks:
  - If used as the top-level architecture, slices can duplicate Dataverse/provider seams and make dependency direction inconsistent.
  - Cross-cutting requirements such as durable state, validation, audit logging, and cloud endpoint selection still need shared ports and policies.

## Consequences

- New backend work must choose a layer deliberately and keep references pointing inward.
- Application ports are the seam for Dataverse, state, validation, logging, configuration, and future providers.
- Infrastructure can change SDKs, storage, hosting adapters, or cloud endpoint logic without forcing Domain changes.
- Presentation remains a control plane and API surface; it does not perform bulk migration execution.
- Tests can cover Domain and Application behavior without requiring Dataverse, ASP.NET Core hosting, or UI frameworks.
- Contributors must add or update ADRs when they need to alter these boundaries; accepted ADRs are immutable and must be superseded by a new ADR.

## Security and Compliance Implications

- Secrets, bearer tokens, SDK clients, endpoint resolution, and cloud-specific authentication remain outside Domain.
- Government and sovereign-cloud behavior is handled through configuration and Infrastructure adapters, not hard-coded into core rules.
- Audit logging and operation state are modeled through Application ports so implementation details can satisfy enterprise requirements without contaminating Domain.
- The API host remains responsible for HTTP concerns such as authentication, authorization, request validation, and composition.

## Follow-up ADR Backlog

- **Persistence and state store:** decide durable job, checkpoint, operation log, and resume storage responsibilities.
- **Migration ordering and dependency resolution:** decide how data and solution component dependencies are planned and sequenced.
- **Validation model:** decide pre-run and post-run validation categories, result severity, reporting contracts, and reconciliation rules.
- **Identity and authorization boundary:** decide supported authentication flows, tenant consent, RBAC, and privileged-operation enforcement.
