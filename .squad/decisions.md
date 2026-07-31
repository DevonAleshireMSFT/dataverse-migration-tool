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

## Governance
- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction


