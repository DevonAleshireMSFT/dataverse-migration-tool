# Squad Decisions

## Active Decisions

### 2026-07-30T15:52:36-07:00: Team casting universe

**By:** Coordinator

**What:** The Squad team for this repository is cast from The Expanse universe: Holden, Naomi, Amos, Alex, Bobbie, Drummer, Prax, and Monica.

**Why:** The theme gives the team a coherent set of recognizable roles and collaboration styles for future routing.

### 2026-07-30T15:52:36-07:00: Public MIT open-source repository

**By:** Coordinator, Drummer

**What:** DevonAleshireMSFT/dataverse-migration-tool is a PUBLIC GitHub repository under the MIT license.

**Why:** The project is intended to be open source, and the MIT license establishes permissive reuse terms.

### 2026-07-30T15:52:36-07:00: @copilot auto-assign enabled

**By:** Coordinator

**What:** @copilot is an autonomous coding-agent member with auto-assign enabled via team.md `copilot-auto-assign: true` and routing entries.

**Why:** Suitable issues can be picked up autonomously by @copilot under the Squad routing and capability rules.

### 2026-07-31: Initial backlog epic structure and release mapping
**By:** Holden
**What:** Structured the initial GitHub backlog around 9 epics: Project Foundations & Governance; Dataverse Connectivity & Environment Intelligence; Migration Engine & Data Movement; Solution Component Migration; Validation, Testing & Quality Gates; Code App UI & Operator Workflow; Security, Identity & Government Readiness; DevOps, Release & Documentation Operations; Extensibility, Configuration & Observability. Mapped foundations to v0.4.0, core provider/migration/UI/validation to v0.5.0, advanced migration/solution/UI work to v0.6.0, and hardening/compliance/release/docs/plugins to v1.0.0.
**Why:** This keeps the backlog at Devon's requested epics + features + spikes depth while preserving clean architecture ownership and Microsoft-supported API guardrails before user-story decomposition.


### 2026-07-31: Dataverse solution publisher and source layout
**By:** Naomi
**What:** Recommend publisher display name `Dataverse Migration Tool`, unique name `DataverseMigrationTool`, customization prefix `dvmig`, option value prefix `10004`; solution unique name `DataverseMigrationTool`, display name `Dataverse Migration Tool`, starting version `0.4.0`; unpacked solution source at `src\solutions\DataverseMigrationTool\` with exported ZIP staging under `artifacts\solutions\`.
**Why:** The publisher prefix permanently stamps Dataverse schema names, so the OSS/government-ready solution needs a neutral, project-owned prefix before components are created. Keeping unpacked solution XML separate from future .NET projects preserves clean architecture ownership and enables supported `pac solution` source-control workflows.


### 2026-07-31: Code App presentation shell initialized
**By:** Alex
**What:** The Power Platform Code App presentation layer lives in `src/app`, uses `pac code` as the CLI workflow, Fluent UI v9 for UI controls and theming, and strict TypeScript plus Prettier for quality gates. Registration was attempted against GFIM-DEV but `pac code init` reported that environment `a1e07a26-233f-eabc-be32-c148767c943d` was not found, so no metadata file was produced and the app has not been pushed or added to the solution yet.
**Why:** This gives the UI a Microsoft-supported Code App baseline while preserving review before any environment push or solution inclusion.


### 2026-07-31T12:40:00-07:00: Local-first Code App development for sovereign cloud
**By:** Squad (Coordinator), for Devon Aleshire
**What:** Code App development is local-first. `pac code init` and `pac code push` to GFIM-DEV (GCC High/UsGovHigh) are deferred because pac 2.6.4's environment lookup is not sovereign-cloud-aware (ref microsoft/PowerAppsCodeApps#331). Registration and add-to-solution will happen once a pac build with working sovereign Code Apps support is validated. Standardize on `pac code`, not the npm `power-apps` CLI. Do not apply the unsupported `node_modules` bypass hack (`government-compatibility`).
**Why:** This keeps development on supported tooling, avoids unsupported package hacks in government cloud, and preserves a clean path for later Dataverse registration once sovereign Code Apps support is validated.

### 2026-07-31: Code App → solution association fails via CLI in DEV
**By:** Squad (Coordinator) — requested by Devon Aleshire
**What:** In the DEV environment (c3d6db85-…, org1ee2d900) with pac 2.10.1, `pac code push` does not persist the code app into a Dataverse solution by any tested CLI path: `--solutionName DataverseMigrationTool`, setting DataverseMigrationTool as preferred solution, or creating a throwaway code app while preferred solution was set. The code app exists only in the Power Apps service layer and has no Dataverse `canvasapp`, `appmodule`, or `solutioncomponent` record. The app source of truth remains `src\app` in Git until solution association works.
**Why:** This appears to be a preview/platform gap in this tenant/environment; recording it prevents repeated unsupported CLI association attempts. Next validation path is portal add-existing for Code Apps, with solution export/import and Pipelines deferred until association works. The throwaway app "ZZ Throwaway Test" should be deleted from DEV.

### 2026-07-31T13:22:14-07:00: Backend scaffold and UI/engine boundary
**By:** Holden
**What:** ADR-001 accepts a firm boundary: the Power Platform Code App is the browser-based admin UI/control plane, and a server-side .NET 9 migration engine owns bulk Dataverse calls, orchestration, resumability, job state, observability, and privileged authentication. The backend layout is `src\backend\DataverseMigrationTool.Domain`, `.Application`, `.Infrastructure`, `.Api`, `.Domain.Tests`, and `.Application.Tests`; Infrastructure owns the Microsoft-supported `Microsoft.PowerPlatform.Dataverse.Client` provider seam; `DataverseMigrationTool.Api\Program.cs` is the DI composition root.
**Why:** Separating the Code App control plane from server-side migration execution preserves clean architecture boundaries, testability, privileged-auth isolation, and supported Dataverse provider composition.

### 2026-08-05T22:54:08-07:00: Product vision & architecture overview
**By:** Holden
**What:** Added a concise product vision and top-level architecture overview as the narrative anchor for secure, repeatable, resumable Dataverse migrations.
**Why:** Issue #10 needs a shared product and architecture reference that aligns the Code App, .NET backend, named subsystems, supported-API principle, and enterprise/government readiness goals.

### 2026-08-05T22:54:08-07:00: Testing strategy & quality gates
**By:** Prax
**What:** Defined the repository testing strategy as a .NET xUnit plus TypeScript/React Vitest pyramid, with hermetic Dataverse fixtures/fakes in CI, opt-in live Dataverse smoke tests only, an initial 80% backend coverage floor, and a 75% Code App coverage floor once Vitest is wired.
**Why:** Migration correctness, relationship preservation, resume/checkpoint, and rollback need explicit automated coverage before Drummer wires CI gates; public PR validation must stay deterministic and free of tenant secrets or live Dataverse dependencies.

### 2026-08-05T22:54:08-07:00: Dataverse provider contracts & auth-handoff seam
**By:** Naomi
**What:** Defined the Dataverse provider connectivity contracts around IDataverseProvider plus the IDataverseTokenProvider auth-handoff seam for Bobbie's #40 implementation. The provider resolves environment-specific DataverseEndpoint values from EnvironmentProfile, honors DataverseCloud for public/GCC/GCC High/DoD authority selection, consumes tokens through IDataverseTokenProvider, and exposes cancellable ConnectAsync, WhoAmIAsync, CheckConnectivityAsync, and ValidateConnectionAsync contracts.
**Why:** Dataverse connectivity must stay on Microsoft-supported Web API/ServiceClient patterns while keeping MSAL, consent, token cache, and credential acquisition out of Naomi's provider layer. Naming IDataverseTokenProvider explicitly gives Bobbie a stable seam for #40 without hard-coded public-cloud endpoints or embedded secrets.

### 2026-08-05T22:54:08-07:00: CI/CD baseline & gates
**By:** Drummer
**What:** Added a baseline GitHub Actions CI design for pull requests and pushes to main: parallel .NET backend build/test and Code App npm lint/build, with no deployment or Power Platform secrets. Documented semver release intent, future coverage gate wiring after #30, and deferred Power Platform deployment automation.
**Why:** Main needs a green, least-privilege validation path before branch protection and release automation can be made boring and auditable. Deployment remains deferred until supported sovereign-cloud tooling and environment promotion requirements are confirmed.

### 2026-08-06T13:09:08-07:00: Configuration provider schema & environment profiles
**By:** Holden
**What:** Added Application-layer migration configuration contracts for distinct source/target Dataverse profiles, secure secret references by name only, validation, and an Infrastructure adapter that reads host configuration with defaults < file < environment < explicit overrides precedence.
**Why:** Migration, Dataverse, and authentication work need a layer-safe source of environment settings without leaking Infrastructure details or storing plaintext secrets in committed configuration.

### 2026-08-06T13:09:08-07:00: MSAL/Entra auth and secret-handling skeleton
**By:** Bobbie
**What:** Added Microsoft.Identity.Client-backed MsalDataverseTokenProvider against Naomi's IDataverseTokenProvider seam, DataverseAuthorityResolver for commercial/GCC/GCC High authority selection, secure default RejectingDataverseDeviceCodePrompt, auth/secret-handling documentation, and Infrastructure.Tests coverage. Secrets remain referenced by configuration and are never stored as plaintext.
**Why:** Dataverse connectivity needs a secure, supported MSAL/Entra authentication seam that works across sovereign clouds while preventing accidental interactive prompts or plaintext secret handling.

### 2026-08-06T14:56:01-07:00: ADR process & ADR-0002 clean-architecture boundaries
**By:** Holden
**What:** Accepted ADR-0002 as the authoritative clean/onion architecture boundary decision and documented the ADR lifecycle: Proposed -> Accepted -> Superseded/Deprecated, with Holden as accepting authority and accepted ADRs immutable except by supersession.
**Why:** The backend already follows Domain -> Application -> Infrastructure/Presentation inward dependencies; recording the rule and process keeps future migration, validation, provider, and UI work aligned before implementation expands.

### 2026-08-06T14:56:01-07:00: Security strategy & threat model
**By:** Bobbie
**What:** Added a decision-ready security strategy and threat model for issue #39, including the ratified government-ready/not-yet-certified compliance posture, secretless-preferred identity, Key Vault reference fallback, redacted audit baseline, and security review gates.
**Why:** Sensitive migration, authentication, storage, logging, and CI/CD work needs an explicit baseline before implementation proceeds, especially for GCC High and sovereign-cloud compatible deployments.

### 2026-08-06T14:56:01-07:00: Government-ready compliance posture (not yet certified)

**By:** Coordinator (on behalf of Devon Aleshire, owner — "make the right decision")

**What:** The dataverse-migration-tool commits to a **government-ready, not-yet-certified** security posture. The tool must be **GCC High / sovereign-cloud compatible by design** — configurable Entra authority/instance and Dataverse endpoints, no public-cloud hardcoding, secretless-by-default auth — but the project makes **no formal FedRAMP / DoD / GCC-High certification claim** at this stage. Supporting decisions:
- **Secret backing store:** Azure Key Vault *reference* is the canonical configuration pattern for any confidential credential; **Entra Workload Identity Federation / managed identity (secretless) is the preferred default** over stored secrets.
- **Audit baseline:** authentication events and migration operations are auditable; tokens, secrets, and PII are redacted by default in all logs.
- **ADR boundaries (#11):** the clean/onion layer boundaries (Domain → Application → Infrastructure → Presentation, dependencies inward, Domain free of SDK/UI) are promoted to an authoritative **ADR-0002**, leaving existing ADR-001 intact. **Holden is the accepting authority**; ADRs are immutable once accepted (a new ADR supersedes rather than edits).

**Why:** This sets a high, defensible engineering bar (sovereign-ready, secure-by-default) appropriate for an early-stage public OSS project, without over-committing to a certification/audit program that is not yet resourced. The posture is explicitly upgradeable later via a superseding ADR once a certification target is funded.




### 2026-08-06T15:10:04-07:00: Backlog taxonomy & release roadmap
**By:** Holden
**What:** Published the backlog taxonomy and release roadmap in `docs/backlog-and-roadmap.md`, using the current GitHub issue labels and the nine-epic structure from the initial backlog decision.
**Why:** Contributors need one authoritative map for work item types, label conventions, release sequencing, and the rule that detailed user stories are decomposed just-in-time per epic rather than up front.

### 2026-08-06T15:10:04-07:00: Definition of Ready & Definition of Done
**By:** Holden
**What:** Added a shared Definition of Ready and Definition of Done that gates work on clear scope, ownership, dependencies, acceptance criteria, risk, testing, documentation, CI, security review, and architecture review.
**Why:** The squad needs a single operational standard for starting and completing features, spikes, docs, and tests, with Bobbie's security checkpoints and Prax/Drummer quality gates folded into the core delivery workflow.

### 2026-08-06T15:10:04-07:00: Coding standards & contribution conventions
**By:** Holden
**What:** Published repository coding standards that reinforce ADR-0002 clean architecture boundaries and OSS contribution conventions for branch naming, commits, PRs, and local validation.
**Why:** Contributors need one authoritative, reviewable reference for C# backend, TypeScript/React Code App, testing, logging, formatting, and contribution workflow expectations before v0.4.0 work expands.

### 2026-08-06T22:08:13-07:00: Code App shell routing foundation
**By:** Alex
**What:** Adopted `react-router-dom` with hash-based client routing for the Code App shell and standardized the initial major workflow sections as Environments & Connections, Metadata Discovery, Compare & Readiness, Validation, Migration Jobs, and Settings & About.
**Why:** The operator UI needs stable client-side navigation that works in hosted Code App/static contexts while preserving clean presentation boundaries and leaving future feature work behind typed application service contracts.

### 2026-08-06: Metadata discovery read models and cache boundary
**By:** Naomi
**What:** Metadata snapshots are plain Domain records under `ValueObjects/Metadata` with tables, fields, relationships, alternate keys, and choices; discovery is exposed through `IMetadataDiscoveryService` and explicit `MetadataDiscoveryRequest/Result` contracts. Infrastructure owns a provider-backed discovery implementation and an in-memory cache decorator registered by `AddMetadataDiscovery`.
**Why:** Compare, validation, and UI need SDK-free, serializable metadata shapes. Caching must be keyed by environment plus normalized scope, TTL-bound, thread-safe, and explicitly invalidatable so schema changes or solution imports do not rely on stale metadata.

### 2026-08-06: Validation report model and rule engine surface
**By:** Prax
**What:** Validation v1 uses an immutable Domain ValidationReport containing ValidationFindings with stable RuleId/Code, message, category, optional target, and severity values Blocker, Warning, and Info. Reports pass only when no Blocker findings exist and expose severity counts plus blocker/warning/info collections. Application owns injectable IValidationRule and IValidationEngine contracts, and Infrastructure owns the rule-based runner plus the Dataverse connectivity validation rule.
**Why:** The validation seam needs deterministic, unit-testable rules and a JSON/UI-friendly report shape that clearly separates migration-blocking failures from operator warnings without leaking provider or UI concerns into Domain.

### 2026-08-06: Environment comparison readiness model
**By:** Naomi
**What:** Environment comparison now treats missing source tables/fields, field type mismatches, stricter target required levels, relationship gaps, missing choices/options, and lookup target mismatches as blockers; alternate-key gaps, looser required levels, and option label differences as warnings; and target-only tables/fields/options/choices as informational. The report exposes severity counts and table-level migration scope readiness so migration selection can include only blocker-free source tables.
**Why:** Dataverse data migration readiness must distinguish schema incompatibilities that can break writes from operator-actionable warnings and harmless target extras while reusing the shared ValidationSeverity model for validation/report consistency.

### 2026-08-06: Migration execution pipeline and run-state seam
**By:** Amos
**What:** Added a separate `IMigrationExecutor` and `IMigrationRunStore` seam for full data migration execution. Execution planning uses metadata relationships to order parents before children, remaps source-to-target ids during load, and defers unresolved/self-referential lookups to a second relationship patch pass. Run state and redacted progress are persisted through the run store and operation logger; record payload values are not logged.
**Why:** Migration execution has to survive failure and retry without hiding state in the process. Keeping execution, data-provider, and run-state contracts in Application and implementations in Infrastructure preserves ADR-0002 boundaries while giving the coordinator a safe DI/API hook to wire.

## Governance
- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
