# Incremental and delta migration strategy

## Purpose

This spike recommends a supported, resumable strategy for incremental Dataverse data migrations after a full baseline run. It does not implement product code. The design stays inside the existing architecture: Application owns migration contracts and state seams, Infrastructure owns Dataverse SDK/Web API mechanics, and the Code App remains only the operator control plane.

## Compliance and API posture

The strategy uses only Microsoft-supported Dataverse mechanisms:

- Dataverse change tracking through the .NET SDK `RetrieveEntityChangesRequest` / `RetrieveEntityChangesResponse` and `BusinessEntityChanges` (`DataToken`, `NewOrUpdatedItem`, `RemovedOrDeletedItem`), or the Web API `Prefer: odata.track-changes` response with `@odata.deltaLink` and `$deltatoken`.
- Dataverse metadata through `EntityDefinitions`, `EntityMetadata.ChangeTrackingEnabled`, `EntityMetadata.CanChangeTrackingBeEnabled`, and `EntityMetadata.Keys` / `EntityKeyMetadata`.
- Dataverse querying through supported SDK `RetrieveMultiple` / `QueryExpression` or Web API entity-set `GET` calls when using `modifiedon` high-water marks.
- Dataverse writes through `UpsertRequest` / `UpsertResponse`, Web API `PATCH` to an entity-set key URL, and, where later validated per table, supported bulk APIs such as `UpsertMultiple`, `CreateMultiple`, and `UpdateMultiple`.

No private service endpoints, direct SQL, undocumented recycle-bin access, tenant-internal APIs, or public-cloud endpoint assumptions are part of this strategy.

## Current migration-engine context

Issue #23 established the full migration execution seam:

- `IMigrationExecutor` orchestrates validation, metadata discovery, plan creation, extraction, load, and relationship patching.
- `IMigrationDataProvider` currently exposes full extraction, batch upsert, and relationship patch operations.
- `IMigrationRunStore` persists run status, table counters, and redacted execution errors.
- `MigrationExecutionPlanner` orders parent tables before children and defers cyclic/self-referential lookup patching.
- `MigrationIdMap` maps source row IDs to target row IDs during a run so lookup patching can converge.

Incremental migration should plug into those seams by adding durable delta-state and changed-record enumeration. It should not move SDK types into Domain or Application.

## Change tracking options

| Option | Supported mechanism | Completeness | Deletes | State to store | Retention / limits | Best use |
| --- | --- | --- | --- | --- | --- | --- |
| Dataverse change tracking | SDK `RetrieveEntityChangesRequest` with `DataVersion`, or Web API `Prefer: odata.track-changes` and `@odata.deltaLink` / `$deltatoken` | Best supported delta signal for tables that enable it. Returns new or updated rows and deleted rows within token retention. | Yes. SDK returns `RemovedOrDeletedItem`; Web API returns `$deletedEntity` entries with `reason: deleted`. | Per source environment, table, selected column set, and migration scope: `DataToken` or opaque delta link, last successful applied checkpoint, page cookie while processing. | Token is valid only while changes remain within the configured change-tracking retention window. Microsoft documents a default of seven days controlled by the Organization `ExpireChangeTrackingInDays` column. Requests track one table at a time in the SDK, and the caller needs organization-level read access. | Default for eligible high-value tables after a full baseline. |
| `modifiedon` high-water mark | SDK `RetrieveMultiple` / `QueryExpression` or Web API `GET` with `$filter` on `modifiedon` and deterministic ordering/tie-breaker | Captures creates and updates that maintain `modifiedon`; easy to reason about and re-scan. Not a true delete feed. | No. Deletes disappear from the table and are missed unless another supported tombstone source is explicitly in scope. | Per table: last completed `modifiedon` timestamp, tie-breaker primary key, selected columns, overlap/replay window. | Clock precision, equal timestamps, async business logic, and plugins require overlap and idempotent replay. Large tables still scan indexes and consume API budget. | Fallback when change tracking is disabled/unavailable and delete propagation is not required, or for operator-approved best-effort refresh. |
| Full re-scan + alternate-key upsert | Full extraction using supported retrieve paging, then target `Upsert` by alternate key | Complete for present rows in selected scope. | No direct delete signal; can reconcile missing target rows only if target rows created/owned by the migration are safely identifiable. | Per table: last full scan time, source row count/checksum summary if later added, key mapping, execution checkpoint. | Most expensive. Subject to service protection limits, table size, plugin cost, and batch-size constraints. | Safe fallback after expired/invalid delta token, schema/key changes, or operator-selected reconciliation. |
| Alternate-key-based upsert | SDK `UpsertRequest` / Web API `PATCH` entity-set alternate-key URL; optional `UpsertMultiple` where supported | Write strategy, not a detection strategy. Makes repeated application converge when keys are stable. | Deletes need separate delete/disable decision. Upsert cannot infer deletes. | Per table: selected key definition, key availability validation, source-to-target id map from upsert response or post-write lookup. | Alternate keys have platform constraints: supported column types, no field-level security on key columns, SQL key size limits, maximum key definitions per table, active index creation required, virtual tables unsupported, and URL-key characters can break retrieve/update/upsert by alternate key. | Default idempotent write mechanism for incremental loads across environments. |

## Recommended default

Use **change tracking first, alternate-key upsert always, and full re-scan as the safe fallback**.

For each selected table:

1. Preflight metadata through `EntityDefinitions` / metadata discovery.
2. If `ChangeTrackingEnabled` is true, use the table's change-tracking feed as the source of incremental rows and deletes.
3. If change tracking is false but `CanChangeTrackingBeEnabled` is true, report an operator action: enable Track changes for future runs. Do not silently enable it unless the operator explicitly asks for a metadata-changing operation.
4. If change tracking is unavailable, disabled, token-expired, or unsupported for the table, fall back to a full re-scan or, only when deletes are explicitly out of scope, `modifiedon` high-water-mark polling.
5. Apply creates and updates with alternate-key-based upsert. Use existing #23 relationship ordering and deferred lookup patching after each changed-table pass.
6. Persist table-level delta state only after the table's changes and relationship patches are successfully applied.

Blunt rule: if we cannot prove the delta feed is complete, we do not pretend it is. We either run a full re-scan or label the result as best-effort and require operator acknowledgement.

## Per-table decision rules

| Condition | Default action |
| --- | --- |
| Table is in selected migration scope, schema has no blockers, and change tracking is enabled | Use `RetrieveEntityChangesRequest` / `DataToken` or Web API delta link. |
| Change tracking token/delta link is missing after a previous full baseline | Run the first change-tracking request as a baseline capture for that table, then persist the returned token only after baseline data is applied. |
| Token is expired or rejected | Mark the incremental path stale and run a full re-scan for that table. Store the new token after convergence. |
| Table cannot enable change tracking | Use full re-scan by default; allow `modifiedon` polling only as an operator-approved no-delete fallback. |
| Table has no usable alternate key in source and target metadata | Block automatic cross-environment incremental upsert unless preserving source primary IDs is explicitly supported for that table and environment. Otherwise require key design/remediation. |
| Alternate key exists but index status is not active | Block writes for that table until the key index is active. |
| Table contains system-owned, virtual, elastic, activity, intersect, or security-sensitive records | Require table-specific validation. Do not assume generic create/update/delete semantics. |
| Relationship target was not in the delta batch but exists from a previous run | Resolve through the durable source-to-target id/key map before deferring relationship patching. |

## Deletes

### Change tracking path

Change tracking is the only recommended default for delete-aware incremental migration. The SDK returns delete markers as `RemovedOrDeletedItem` entries in the `BusinessEntityChanges` collection. The Web API delta response returns deleted rows as `$deletedEntity` entries with `reason: deleted`.

Recommended handling:

1. Persist delete events as part of the table delta page state before applying them.
2. Resolve the target row by the durable source-to-target id map or alternate key.
3. Apply the configured delete policy:
   - **Mirror delete** for tables where target rows are migration-owned and deletion is approved.
   - **Deactivate/state transition** where Dataverse table semantics or operator policy prefer soft delete.
   - **Report only** for sensitive/system tables or when target may contain independent production edits.
4. Record redacted audit entries with job ID, table, source row ID hash or stable ID, target row ID when safe, operation, and result. Do not log row payload values.
5. Advance the stored token only after delete handling succeeds or is explicitly skipped by policy.

If a source row is created and deleted between delta polls, Microsoft documents that the client can receive the deleted item even if it never saw the created row. That is not an error. The executor should treat a missing target mapping during delete as idempotent success when the configured policy is mirror-delete and the target row cannot be found.

### `modifiedon` path

`modifiedon` polling does not see deletes. It must not be advertised as delete-complete. For tables using this fallback, the run report must say one of:

- deletes are unsupported for this table's incremental mode;
- deletes require a full target reconciliation pass; or
- deletes are intentionally out of scope by operator policy.

A full re-scan can identify target rows that no longer exist in source only if the tool can prove those target rows are migration-owned and can compare by durable alternate key/source identity. Even then, the default action should be report-and-confirm, not destructive delete.

## Alternate keys and upsert

Alternate keys are the default identity bridge across environments. They let the tool identify a target row by stable business identity instead of assuming source GUIDs can always be reused.

Design rules:

- Prefer a table-specific alternate key that exists and is active in both source and target metadata.
- Validate key columns are included in extraction and are not null for migrated rows.
- Do not include alternate-key columns redundantly in the update body when using the same key to identify the record; Microsoft documents that Dataverse treats key values specially during `Upsert`.
- Store the target row ID returned by `UpsertResponse.Target`, or retrieve the primary ID through the supported Web API/SDK path when using Web API `PATCH` without representation.
- Feed that source-to-target mapping into the existing #23 lookup remapping and deferred relationship patch pass.
- For Web API upsert, use `PATCH [entityset](key=value)` without `If-Match: *` for upsert. Use `If-Match: *` only for update-only behavior and `If-None-Match: *` only for create-only behavior.
- Consider `Prefer: return=representation` only when the implementation needs the primary key in the response and has measured the extra retrieve cost; otherwise favor SDK `UpsertResponse` or a separate minimal `$select` lookup.

When no durable alternate key exists, the safe backlog is to add one through solution metadata or table configuration before incremental migration. Falling back to names or non-unique columns is how duplicate records happen.

## Conflict handling

Incremental migration needs an explicit conflict policy per job or per table. Defaults should be conservative.

| Policy | Behavior | When to use | Risk |
| --- | --- | --- | --- |
| Source wins | Apply source changed values to target through upsert, overwriting target fields in scope. | Rehearsal targets, one-way sync, pre-cutover loads where target is not independently edited. | Can overwrite target-side changes. |
| Target wins | Skip source update when target changed after the last applied migration checkpoint. | Production targets where operators may make approved changes during a migration window. | Source and target diverge; skipped rows need report/remediation. |
| Timestamp comparison | Compare source `modifiedon` to target `modifiedon` or stored last-applied timestamp, then apply the newest or flag ties. | Low-complexity tables where timestamps are trustworthy and both sides use comparable Dataverse semantics. | Clock/automation semantics are not a business conflict model. Plugins can modify timestamps. |
| Operator decision | Pause or mark rows/tables as conflict-blocked with a report. | Deletes, relationship conflicts, records modified on both sides, restricted/system tables, or any untrusted policy. | Slower, but honest and recoverable. |

Recommended default for v1 incremental implementation: **source wins for rehearsal/non-production targets only; operator decision for deletes and target-modified conflicts; target wins only when explicitly configured**.

The state store should persist enough redacted state to decide conflicts without storing row payloads: source row ID/key, target row ID, last applied source token/timestamp, last applied target row version or `modifiedon` when captured, field scope, and policy result.

## Fallback behavior

Fallback is table-scoped, not all-or-nothing:

1. Try change tracking for eligible tables.
2. If unavailable before first incremental run, choose full re-scan and persist a clear reason.
3. If token retention expired, run full re-scan for that table and replace the token only after successful convergence.
4. If a table is small and deletes are not required, allow `modifiedon` high-water-mark polling with overlap replay. The overlap window must be idempotent through alternate-key upsert.
5. If fallback would be destructive or incomplete, stop that table and require operator decision.

Fallback runs still respect service protection limits. They should reuse #23 paging, batching, retry, progress, and checkpoint patterns rather than inventing a second execution path.

## Government and sovereign-cloud constraints

There is no separate unsupported endpoint needed for this strategy. The same supported Dataverse SDK/Web API operations must be called against the configured organization URI for the selected environment. For GCC High and other sovereign deployments:

- Resolve Dataverse environment URLs and Entra authority hosts from environment profiles, never from hardcoded commercial-cloud constants.
- Keep source and target cloud/tenant boundaries explicit in job state and audit events.
- Do not claim FedRAMP, DoD, or GCC High certification; the project posture remains government-ready, not certified.
- Keep tokens, delta links, row data, and PII out of logs and browser state. Delta tokens/links are operational state and should be protected like migration checkpoint data.
- Any cross-cloud migration should remain gated by future Bobbie/Naomi policy work; incremental mode must not weaken that gate.

## Design-level integration with existing seams

Future implementation should add contracts without changing clean-architecture direction:

- Application contracts for `MigrationMode.Full` vs incremental/delta, table delta state, delete policy, conflict policy, and fallback reason.
- An Application port such as `IMigrationDeltaProvider` that returns changed records and delete markers using SDK-free records.
- Infrastructure implementations backed by `RetrieveEntityChangesRequest` or Web API delta links, plus a `modifiedon` polling adapter and full re-scan adapter.
- `IMigrationRunStore` or a sibling durable state store extension for per-table delta tokens, high-water marks, page cookies, id/key maps, conflict decisions, and safe checkpoint transitions.
- `IMigrationExecutor` orchestration that chooses a table strategy after metadata validation, applies changed records through the existing upsert/relationship patch flow, handles deletes through policy, then advances the token after successful application.
- Metadata/compare validation that blocks or warns on missing alternate keys, disabled change tracking, inactive key indexes, unsupported tables, and delete-policy gaps.

The critical invariant: **never advance a delta token before the corresponding writes, deletes, relationship patches, and run-state checkpoint are durably recorded**.

## Risks and unsupported cases

- Change tracking token expiry can force expensive full re-scans. Default retention is seven days unless the organization setting changes.
- Change tracking must be enabled per table and cannot be enabled for every table. Some tables are ineligible.
- Enabling change tracking is a metadata/configuration change and, once enabled, Microsoft documents it cannot be disabled for that table. Operator approval is required.
- `RetrieveEntityChanges` requires organization-level read access to the table. Least-privilege roles must be validated before relying on it.
- Web API change tracking does not support `$filter`, `$orderby`, `$expand`, or `$top` with `Prefer: odata.track-changes`; the tracked query shape must be simple and stable.
- `modifiedon` polling misses deletes and can miss/duplicate edge cases without overlap and tie-breakers.
- Alternate keys are not available for virtual tables and have column type, field-security, key-size, key-count, index-status, and character constraints.
- Elastic table `Upsert` behavior differs from standard tables and can bypass `Create`/`Update` event expectations; it needs table-specific validation.
- Plugins, flows, calculated columns, rollups, and async business logic can change target rows after write and create conflict/noise in timestamp policies.
- Delete mirroring can destroy target-only data if migration ownership is not proven. Default to report/decision.
- Many-to-many/intersect table deltas need explicit handling; generic row upsert may not preserve relationship semantics.
- Attachments, file/image columns, audit table data, users, teams, business units, security roles, and other system/security-sensitive tables are not automatically covered by this generic delta strategy.
- Cross-tenant or cross-cloud migrations may have data-residency and authorization constraints beyond migration mechanics.
- High-volume delta catch-up can still hit service protection limits; the executor must honor `Retry-After`, backoff, batch size, cancellation, and checkpoint/resume.

## Proposed implementation backlog

- [ ] **Add delta-state contracts and storage** — Define SDK-free table delta state, token/high-water mark checkpoints, conflict/delete policy, and durable run-store persistence.
- [ ] **Implement Dataverse change-tracking delta provider** — Add Infrastructure support for `RetrieveEntityChangesRequest` or Web API delta links, including paging, token retention errors, and delete markers.
- [ ] **Add alternate-key upsert validation** — Validate source/target key availability, active index status, key column extraction, and unsupported key shapes before incremental execution.
- [ ] **Wire incremental executor mode** — Extend `IMigrationExecutor` orchestration to choose per-table delta strategy, reuse #23 upsert/remap/relationship patching, and advance tokens only after durable success.
- [ ] **Implement delete policy handling** — Support mirror-delete, deactivate/report-only policy decisions with idempotent missing-target behavior and redacted audit events.
- [ ] **Add modifiedon fallback provider** — Implement high-water-mark polling with overlap replay, deterministic tie-breakers, and explicit no-delete reporting.
- [ ] **Add full re-scan fallback and reconciliation reports** — Reuse full extraction when tokens expire or change tracking is unavailable, and report target rows not present in source without destructive default behavior.
- [ ] **Add conflict detection and operator decision model** — Track last-applied source/target versions, detect target-modified rows, and surface source-wins/target-wins/operator-choice outcomes.
- [ ] **Add service-protection-aware delta throttling** — Honor Dataverse `Retry-After`, tune batch/concurrency per table, and make throttling visible in progress and run reports.
- [ ] **Add delta validation and regression tests** — Cover token expiry, create/update/delete deltas, fallback paths, duplicate replay, relationship remap, conflict policies, and sovereign endpoint configuration with fakes by default.
- [ ] **Document operator runbook for incremental migration** — Explain prerequisites, enabling change tracking, retention windows, delete policies, conflict choices, and recovery from stale tokens.

## References

- Microsoft Learn: Use change tracking to synchronize data with external systems (`RetrieveEntityChangesRequest`, `DataToken`, Web API `Prefer: odata.track-changes`, `@odata.deltaLink`, `$deltatoken`, `$deletedEntity`).  
  https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-change-tracking-synchronize-data-external-systems
- Microsoft Learn: `RetrieveEntityChangesRequest` class (`EntityName`, `Columns`, `DataVersion`, `PageInfo`, `RetrieveEntityChangesResponse.EntityChanges`).  
  https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.retrieveentitychangesrequest?view=dataverse-sdk-latest
- Microsoft Learn: Work with alternate keys (`EntityKeyMetadata`, `CreateEntityKey`, `RetrieveEntityKeyRequest`, `DeleteEntityKeyRequest`, `ReactivateEntityKey`, key constraints).  
  https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
- Microsoft Learn: Use Upsert to create or update a record (`UpsertRequest`, `UpsertResponse`, Web API `PATCH`, `If-Match`, `If-None-Match`, `Prefer: return=representation`).  
  https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-upsert-insert-update-record
- Microsoft Learn: Service protection API limits (`429 Too Many Requests`, SDK `OrganizationServiceFault`, `Retry-After`).  
  https://learn.microsoft.com/en-us/power-apps/developer/data-platform/api-limits

## Recommendation summary

Default to Dataverse change tracking for eligible tables because it is the supported mechanism that captures creates, updates, and deletes with a durable delta token. Apply changes idempotently through alternate-key upsert and the existing #23 id-remapping/relationship patch flow. When change tracking is unavailable or stale, fall back to full re-scan; use `modifiedon` polling only as an explicit best-effort, no-delete fallback.


