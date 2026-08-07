# Solution component migration surface

## Decision posture

This spike defines the supported and safe solution-component migration surface for the Dataverse Migration Tool. The recommendation is deliberately conservative: move solution-aware metadata through Microsoft-supported ALM paths, treat Dataverse data migration as the separate engine owned by #23, and never depend on undocumented service endpoints or hand-edited internals.

The tool remains **government-ready, not yet certified**. It must work from configurable environment profiles for commercial, GCC, GCC High, DoD, and future sovereign clouds without hard-coded public endpoints.

## Supported Microsoft tooling and APIs

Use these surfaces only:

| Surface | Supported operations | Use in this product |
| --- | --- | --- |
| Power Platform CLI `pac solution` | `pac solution add-solution-component`, `clone`, `export`, `import`, `pack`, `unpack`, `sync`, `upgrade`, `publish`, `create-settings`, and `check` | Preferred local/operator wrapper for solution projects, export/import, packaging, solution checker, and settings files. |
| Power Platform Build Tools | Tool installer, WhoAmI, Power Platform Checker, Export Solution, Import Solution, Pack/Unpack Solution, Apply Solution Upgrade | Preferred CI/CD orchestration surface when Azure Pipelines or GitHub Actions-compatible wrappers are introduced. |
| Dataverse Solution APIs | Web API / SDK actions `ExportSolution`, `ImportSolution`, `StageAndUpgrade`, `AddSolutionComponent`; import monitoring through `ImportJob` / formatted import results | Preferred server-side automation surface when the migration engine must run a controlled export/import, capture correlation IDs, poll status, and return redacted diagnostics. |
| Dependency APIs | `RetrieveDependenciesForDelete`, `RetrieveDependenciesForUninstall`, dependency table reads, and import log diagnostics | Preflight and explain dependency blockers; do not bypass dependency enforcement. |
| ALM guidance | Custom publisher, unmanaged source in development, managed artifacts downstream, managed upgrade/holding solution behavior, solution settings for environment-specific values | Governs recommended lifecycle and operator warnings. |

Specific references:

- `pac solution` command group: <https://learn.microsoft.com/en-us/power-platform/developer/cli/reference/solution>
- `pac auth create --cloud Public|UsGov|UsGovHigh|UsGovDod|China`: <https://learn.microsoft.com/en-us/power-platform/developer/cli/reference/auth>
- Power Platform Build Tools tasks: <https://learn.microsoft.com/en-us/power-platform/alm/devops-build-tool-tasks>
- Solution ALM concepts: <https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm>
- Solution APIs: <https://learn.microsoft.com/en-us/power-platform/alm/solution-api>
- Web API actions: `ExportSolution`, `ImportSolution`, `StageAndUpgrade`, `AddSolutionComponent`
- Solution component type values: <https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent>

## Component coverage matrix

Status meanings:

- **Supported** — safe to migrate through solution export/import using supported tooling, with dependency validation.
- **Conditional** — supported by Microsoft tooling, but this product must add preflight checks, settings, cloud availability checks, or operator steps before treating it as safe.
- **Unsupported or deferred** — not safe for MVP migration automation, not solution-aware, requires tenant-specific provisioning, or relies on unavailable/undocumented behavior.

| Component type | Solution component examples | Status | Notes |
| --- | --- | --- | --- |
| Tables/entities | `Entity` (1) | Supported | Include custom tables and safe table metadata. Data rows are not solution components and remain in the data migration engine (#23). |
| Columns/attributes | `Attribute` (2), attribute picklist values (4), lookup values (5) | Supported | Safe when parent table exists and source/target metadata dependencies are satisfied. Required-level changes can block data migration readiness (#21). |
| Relationships | `Relationship` (3), `Entity Relationship` (10), roles and relationship subcomponents | Supported | Import order is dependency-driven by Dataverse. Data relationship population remains #23's parent/child and lookup patch logic. |
| Choices/optionsets | `Option Set` (9), local choice values | Supported | Labels/localization are solution metadata. Deleting or renumbering choices can affect existing data and must be warned. |
| Alternate keys | `Entity Key` (14) | Supported | Important complement to #23 upsert/idempotency. Activation state can be asynchronous; validation must check key availability before data loads. |
| Forms | `System Form` (60), `Form` (24) | Supported | Import through solutions only; do not hand-create form XML in target. |
| Views | `Saved Query` (26), view attributes (6) | Supported | Personal views are not covered; system views in solutions are. |
| Charts | `Saved Query Visualization` (59) | Supported | Safe as solution metadata when dependent table/view metadata exists. |
| Dashboards | System forms / app artifacts | Supported | Treat as solution metadata and validate dependent charts/views. |
| Business rules | `Workflow` (29) category business rule | Supported | Imported as solution process metadata. Activation/publish behavior must be controlled and logged. |
| Classic workflows/processes | `Workflow` (29) | Conditional | Supported in solutions, but runtime activation, ownership, and disabled/deprecated process patterns require preflight warnings. |
| Power Automate cloud flows | `Workflow` (29), connection references | Conditional | Supported only for solution-aware flows. Requires connection references, valid target connections, owner/permission checks, and connector availability in the target cloud. Flow run history is excluded. |
| Security roles | `Role` (20), role privileges (21), privileges (16) | Conditional | Roles are solution components, but user/team assignments are data/security administration and are excluded. Imported privileges depend on table components and target licenses/features. |
| Field security profiles | `Field Security Profile` (70), field permissions (71) | Conditional | Profile metadata can move; user/team membership and operational access review are excluded. |
| Plug-ins | `Plugin Assembly` (91), `Plugin Type` (90), SDK message processing step/image (92/93), service endpoint (95) | Conditional | Supported by solution ALM, but requires supported assembly registration/package process, target permissions, endpoint/cloud availability, and no secrets in secure/unsecure configuration. Runtime side effects must be warned before data migration. |
| Web resources | `Web Resource` (61) | Supported | Static assets move in solution packages. Validate size and solution checker output. |
| Model-driven apps | App modules, site maps (62), forms/views/charts | Supported | Safe when all dependent components are in the solution or already installed. |
| Canvas apps | `Canvas App` (300), connection references | Conditional | Supported for solution-aware canvas apps. Requires target connections, connection references, and connector availability. Non-solution-aware apps are excluded. |
| Power Platform Code Apps | Code App source plus eventual solution component | Unsupported or deferred | Current team decision keeps Code App development local-first. `pac code push`/solution association is deferred until supported sovereign behavior is validated. |
| Connection references | Connection reference solution components | Conditional | The reference migrates; the connection credential does not. Target connections must be supplied or mapped during import and may require sharing with the flow/app owner. |
| Environment variables | Definition (380), value (381) | Conditional | Definitions migrate. Values must be supplied through solution settings/import parameters or approved environment-specific files. Current values and secrets must not be committed. |
| Environment variable secrets | Secret-type environment variables backed by Azure Key Vault | Conditional | Use Key Vault references or managed identity/workload identity. The solution must not carry plaintext secret values. |
| Custom connectors / connector components | Connector (371/372) | Conditional | Supported only where the connector and dependent policies are available in the target cloud. Sovereign availability and DLP policy checks are mandatory. |
| Reports and templates | Reports (31-34), email templates (36), document/contract templates | Conditional | Supported as solution components, but may depend on deprecated features, external data sources, or tenant-specific settings. |
| Duplicate detection, SLAs, routing/conversion rules | Duplicate rules (44/45), SLA/SLA Item (152/153), routing/conversion rules | Conditional | Supported metadata but often feature/license and activation dependent. Validate target feature availability. |
| Mobile/offline profiles | Mobile offline profile/item (161/162) | Conditional | Supported metadata, but app/mobile configuration and offline profile availability must be checked per target. |
| Organization/settings components | Organization (25), exported settings such as autonumbering/general/customization | Conditional | Only include explicit supported settings flags (`pac solution clone --include ...`, `ExportSolution` settings flags). Treat org-wide changes as high-risk operator choices. |
| System users, teams, business units, role assignments | Data/security records, not ordinary solution metadata | Unsupported or deferred | Use tenant administration and data-migration-specific logic later if approved. Do not include in solution-component MVP. |
| Record data, attachments, audit history, flow run history | Runtime data | Unsupported or deferred | Covered by #23 or explicitly out of scope. Solutions move metadata/customizations, not tenant data history. |
| Default solution / unmanaged target edits | Default solution contents, ad hoc target customization | Unsupported or deferred | Use custom publisher and named solutions. Do not automate broad default solution export/import. |
| Internal or undocumented component operations | Private APIs, service-layer records, hand-edited zip internals | Unsupported or deferred | Explicitly prohibited. |

## Dependency and ordering behavior

Dataverse solutions are dependency-aware packages. Tables contain nested components such as columns, forms, views, charts, relationships, messages, and business rules. Import succeeds only when required base solutions and required components are present in the target environment or included in the package.

Safe behavior for this tool:

1. Build or select an unmanaged source solution that contains the intended root components.
2. Use `AddSolutionComponent` / `pac solution add-solution-component` only against an unmanaged source solution, and decide explicitly whether to add required components.
3. Export/import the solution as the unit of movement; let Dataverse enforce dependency ordering.
4. Capture `ImportJobId`, import log output, and missing-dependency diagnostics for the validation report.
5. Use dependency APIs to explain blockers before destructive operations or managed uninstalls. `RetrieveDependenciesForDelete` explains delete blockers; `RetrieveDependenciesForUninstall` explains managed-solution uninstall blockers.
6. Never skip or suppress dependencies to force an import. `SkipProductUpdateDependencies` and internal parameters are not MVP surfaces.

Missing dependencies should be reported as blockers with component type, object ID/name when available, required solution/base component, and a recommended operator action: install the base solution, add the required component to scope, or defer that component.

## Managed vs unmanaged posture

Recommended posture:

- **Source/development:** unmanaged solution under a custom publisher (`Dataverse Migration Tool`, prefix `dvmig` per existing decision) is the authoring/source container.
- **Downstream migration target:** prefer **managed** solution imports for repeatable non-development target environments, including test, UAT, production, and government tenants.
- **Operator rehearsal/sandbox:** unmanaged import can be allowed only when the operator intentionally wants editable metadata in a development-like target.

Implications:

- Managed layers are auditable and uninstallable as a unit, but direct edits create unmanaged layers and dependencies that can block uninstall or upgrade.
- Managed updates cannot delete removed components; managed upgrades/holding solutions are required for removal and cleanup.
- `ImportSolution` with `HoldingSolution=true` and `StageAndUpgrade` / `pac solution upgrade` are the supported paths for staged upgrades.
- Patches are useful for hotfix-style changes but cannot delete components; do not make patch orchestration the MVP.
- Deletes are dangerous in migration contexts because managed solution uninstall can remove custom-table data and columns. Surface delete/upgrade consequences before execution.

## Government and sovereign constraints

Government readiness changes endpoint and tooling assumptions, not the supported surface:

- Environment profiles must carry cloud, tenant ID, authority host, Dataverse URL/resource, and target environment ID. No commercial endpoint fallback.
- `pac auth create --cloud` supports `Public`, `UsGov`, `UsGovHigh`, `UsGovDod`, and `China`; GCC maps to `UsGov`, GCC High to `UsGovHigh`, and DoD to `UsGovDod`.
- `pac solution` and Build Tools automation must select the correct environment URL/ID and cloud-specific auth profile or service connection.
- Solution Checker geo/endpoint must be configurable for government geographies where used.
- Connector, flow, canvas app, Code App, and custom connector availability can differ from commercial. Treat unavailable connectors/features as blockers, not as reasons to call private APIs.
- Keep the existing local-first Code App decision: do not depend on `pac code push` or solution association in GCC High until supported sovereign behavior is validated.
- Cross-cloud moves can create residency issues. Default to blocking cross-boundary solution migration unless a future security decision adds an explicit operator gate.

## Connection references, environment variables, and secrets

Safe migration rules:

1. Connection references are solution components; actual connections and OAuth credentials are not portable secrets. On import, target connections must be provided, mapped, or created by an authorized operator/service identity.
2. Flow/app enablement requires the enabling user or service principal to own or have permission to use referenced connections.
3. Environment variable definitions move in solutions. Values should be supplied by `pac solution create-settings`, Build Tools deployment settings, or `ImportSolution` / `StageAndUpgrade` `ComponentParameters` where supported.
4. Commit environment-specific settings only when they contain non-secret placeholders. Never commit tokens, passwords, client secrets, connection strings, device codes, certificates, or tenant-sensitive values.
5. Secret environment variables must use Azure Key Vault reference patterns or secretless managed identity/workload identity. The solution package must not become a secret transport.
6. Plug-in secure/unsecure configuration, connector policies, and flow parameters must be scanned for secret-like values before export and redacted from logs/import diagnostics.

## Recommendation

MVP should target a **solution-container orchestration** model, not per-component target mutation. The tool should help operators select or assemble an unmanaged source solution, validate dependencies and target readiness, export a managed solution artifact, import it into the target with tracked `ImportJobId`, and report blockers using solution import/dependency diagnostics.

Preferred execution order:

1. Use metadata discovery (#20) and environment compare/readiness (#21) to identify schema dependencies, target blockers, and data-migration impact.
2. Use `pac solution` for local operator workflows and Build Tools for pipeline workflows.
3. Use direct Dataverse Solution APIs from the backend only when the product must orchestrate server-side export/import/status in a resumable, auditable job.
4. After solution metadata succeeds, hand off data movement to #23 so data loads run against target schema that has already passed solution readiness.

### Supported MVP surface

- Custom named solutions under the project publisher.
- Tables, columns, relationships, choices, alternate keys, forms, views, charts, dashboards, web resources, model-driven apps, business rules, and supported solution-aware metadata.
- Conditional support for security roles, flows, canvas apps, plug-ins, connection references, environment variables, and settings when preflight validates dependencies, target availability, owners/connections, and secret handling.
- Managed export/import for non-development targets, with import job tracking and redacted diagnostics.

### Exclude or defer

- Code App solution association until supported sovereign `pac code` behavior is validated.
- Default solution export/import and broad unmanaged target overwrites.
- User/team/business unit provisioning, role assignments, flow run history, audit history, attachments, and row data.
- Private APIs, unsupported package edits, or direct target writes for components that should move through solution ALM.
- Automated delete/managed uninstall/patch cleanup beyond warning and dependency reporting.

## Risks and unsupported scenarios

| Risk/scenario | Decision |
| --- | --- |
| Missing base solution or dependency blocks import | Report blocker; do not bypass dependency checks. |
| Managed upgrade removes components or data | Require explicit operator warning and future rollback guidance before implementation. |
| Target has unmanaged customizations over managed components | Warn; default not to overwrite unmanaged customizations without explicit policy. |
| Connectors unavailable in GCC High/DoD | Block or defer affected components. |
| Connection references lack target connections | Block import/activation until mapped. |
| Environment variable values contain secrets | Reject plaintext; require Key Vault reference or secretless identity. |
| Plug-ins mutate data during #23 migration | Warn and require operator decision to disable, sequence, or account for side effects. |
| Solution package exceeds platform limits or checker fails | Block MVP import until package is split or issues accepted by policy. |
| Cross-cloud migration | Default blocked pending security/compliance gate. |
| Unsupported APIs requested by users | Refuse and document unsupported status. |

## Proposed implementation backlog

- [ ] **Define solution migration Application contracts** — Add request/result models for solution export/import planning, dependency blockers, and import status without leaking SDK types.
- [ ] **Implement Solution API adapter** — Wrap `ExportSolution`, `ImportSolution`, `StageAndUpgrade`, `AddSolutionComponent`, and import-job polling in Infrastructure.
- [ ] **Add solution dependency preflight** — Use solution component metadata, dependency APIs, and import-log parsing to produce blocker/warning findings.
- [ ] **Add supported component coverage rules** — Encode the MVP matrix as validation rules with Supported / Conditional / Deferred classifications.
- [ ] **Add managed/unmanaged policy options** — Let operators choose managed downstream imports by default and require warnings for unmanaged target imports or overwrite behavior.
- [ ] **Add connection reference and environment variable settings workflow** — Generate/import settings files and reject plaintext secrets.
- [ ] **Add sovereign-cloud solution tooling profile validation** — Validate `pac` cloud/auth profile, environment URL, checker geo, and connector availability before export/import.
- [ ] **Add solution import job audit events** — Record import job ID, solution name/version, source/target aliases, status, duration, and redacted failure categories.
- [ ] **Add plug-in/process side-effect readiness checks** — Warn when migrated components can execute during data migration and require operator sequencing decisions.
- [ ] **Spike Code App solution association in sovereign clouds** — Re-test supported `pac code` / portal association paths before including Code Apps in solution migration automation.
