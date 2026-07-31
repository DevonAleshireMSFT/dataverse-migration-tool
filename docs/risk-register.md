# Risk Register — dataverse-migration-tool

> **Last reviewed:** 2026-07-31  
> **Review cadence:** Sprint retrospective (bi-weekly) or on any P0/P1 incident  
> **Owner:** Devon Aleshire (product owner) + Holden (lead) + Prax (quality gate)  
> **Closes:** #32 · Parent: #5

---

## How to Use This Register

| Column | Definition |
|--------|------------|
| **ID** | Stable identifier. Never reuse. |
| **Area** | Architecture · Dataverse API · Security · Government · Migration Correctness · Performance · OSS · Delivery |
| **Likelihood** | L = Low · M = Medium · H = High · C = Critical |
| **Impact** | L = Low · M = Medium · H = High · C = Critical |
| **Score** | Likelihood × Impact: L×L = 1, L×M = 2, M×M = 4, H×H = 9, C×C = 16 (see matrix below) |
| **Owner** | Squad member who tracks and mitigates |
| **Trigger** | Observable signal that activates the mitigation plan |
| **Mitigation** | Concrete action(s) to reduce or accept the risk |
| **Issue** | Linked GitHub issue(s) for tracking |

### Risk Score Matrix

|  | **L Impact** | **M Impact** | **H Impact** | **C Impact** |
|---|---|---|---|---|
| **L Likelihood** | 🟢 1 | 🟢 2 | 🟡 3 | 🟠 4 |
| **M Likelihood** | 🟢 2 | 🟡 4 | 🟠 6 | 🔴 8 |
| **H Likelihood** | 🟡 3 | 🟠 6 | 🔴 9 | 🔴 12 |
| **C Likelihood** | 🟠 4 | 🔴 8 | 🔴 12 | 🔴 16 |

> 🟢 = Accept / Monitor · 🟡 = Plan mitigation · 🟠 = Active mitigation required · 🔴 = Blocker — requires immediate resolution or explicit acceptance by owner

---

## Risk Register

### Architecture Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **ARCH-01** | Infrastructure dependencies leak into the Domain layer (Dataverse SDK references, EF types, HTTP clients imported directly into domain models or domain services) | H | H | 🔴 9 | Holden | A Domain or Application project gets a direct `PackageReference` to a Dataverse or Azure SDK | ADR-0001 enforces clean-architecture layer boundaries. Build-time analyzer (ArchUnitNET or custom Roslyn rule) fails CI if dependency direction is violated. | #11 #12 |
| **ARCH-02** | Circular or tangled dependencies between the Migration Engine, Validation Engine, and Dataverse Provider seams cause integration brittleness | M | H | 🟠 6 | Holden | Cross-layer method calls that do not pass through a defined contract interface | Define all inter-engine contracts as C# interfaces in the Application layer before implementation begins. Holden reviews every new cross-seam call. | #22 #31 |
| **ARCH-03** | Over-engineering early scaffolding (e.g., premature plugin framework, generic abstractions before patterns emerge) delays delivery of v0.4 milestone | M | M | 🟡 4 | Holden | Sprint velocity drops below 60 % of planned story points for two consecutive sprints | Apply YAGNI gate at sprint review. Holden and Devon decide to defer extensibility work to v1.0 unless a concrete requirement exists. | #51 |
| **ARCH-04** | DI composition root grows monolithic; registrations for dev, GCC, and production environments diverge without a tested profile strategy | M | M | 🟡 4 | Holden | An environment-specific registration is copy-pasted rather than expressed through a profile or feature flag | Configuration Provider schema (#48) defines environment profiles. Integration tests verify each profile independently. | #48 |

---

### Dataverse API Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **API-01** | Dataverse API throttling (429 / service-protection limits) causes migration job to abort mid-run against large tables or after sustained bursts | H | H | 🔴 9 | Naomi + Amos | HTTP 429 or `Retry-After` header received during a migration execution; sustained retry loops > 5 min | Implement Retry + exponential back-off in the Dataverse Provider. Honour `Retry-After`. Surface throttle events in the Validation Report. Expose batch-size tuning in the Configuration Provider. | #18 #20 #22 |
| **API-02** | Undocumented or unsupported API behaviours surface only in GCC/GCC-High environments (different endpoint base URIs, token audience, feature flags) | M | H | 🟠 6 | Naomi + Bobbie | A test that passes against a commercial environment fails against a GCC or GCC-High tenant | Build an environment-profile abstraction (endpoint URIs, audiences) in the Dataverse Provider from day one. Spike against a GCC tenant before v0.5. | #18 #41 |
| **API-03** | Metadata discovery exhausts API call budget on tenants with large solution portfolios (thousands of entities, relationships, plugins) | M | M | 🟡 4 | Naomi | Metadata fetch takes > 30 s or produces > 1 000 API calls per run | Implement metadata caching with configurable TTL. Provide incremental refresh strategy. | #20 |
| **API-04** | Breaking changes to the Dataverse Web API (new API version requirements, deprecated endpoints) break the migration tool post-release | L | H | 🟡 3 | Naomi | Microsoft deprecation announcement or 410 response from a previously-working endpoint | Pin to a tested API version. Monitor the Power Platform release notes feed. Integration contract tests run on every release to catch regressions. | #18 #27 |
| **API-05** | Bulk record operations (upsert/create in migration) silently skip records when payload exceeds OData batch limits or server-side max page size | M | H | 🟠 6 | Amos | Record count in destination after migration does not match source; silent gaps in validation report | Validate batch sizes against documented limits. Assert source ↔ destination row counts in the Validation Engine. Write a regression test for each known limit boundary. | #23 #31 |

---

### Security Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **SEC-01** | Service principal client secrets or refresh tokens logged in plain text to console, file appenders, or GitHub Actions step logs | M | C | 🔴 8 | Bobbie | Secret scanning alert or manual code review finds a credential value in a log statement or structured-log field | Logging framework configured to redact fields matching `*secret*`, `*token*`, `*password*`, `*key*` before output. Secret-handling skill enforced in CI. Bobbie reviews every change to the auth or logging subsystems. | #39 #40 #50 |
| **SEC-02** | Migration payloads contain PII or restricted data that is inadvertently written to checkpoint files, logs, or validation reports | M | H | 🟠 6 | Bobbie | A checkpoint file or report on disk contains recognisable PII (email, SSN, name pattern) | Define a data-classification field in the migration scope configuration. Checkpoint and report writers redact classified fields. Prax adds a PII-scrub regression test. | #25 #39 |
| **SEC-03** | MSAL token cache persisted to disk is readable by other processes or remains on operator workstation after session | L | H | 🟡 3 | Bobbie | Token cache file found with world-readable permissions; cache not cleared on disconnect | Use OS-native credential store (Windows Credential Manager / macOS Keychain / Linux Secret Service). Never write raw tokens to the file system. | #40 |
| **SEC-04** | Overly broad Dataverse roles assigned to the migration service principal allow destructive operations (bulk delete, system customizer) beyond migration scope | M | H | 🟠 6 | Bobbie | Service principal has System Administrator or System Customizer role in a production environment | Document least-privilege role requirement (custom security role restricted to migrated entities). Provide a role-check pre-flight gate in the tool. | #39 #42 |
| **SEC-05** | Third-party OSS libraries (NuGet, npm) introduce a transitive dependency with a known CVE | M | M | 🟡 4 | Drummer + Bobbie | Dependabot alert or `dotnet list package --vulnerable` returns a finding | Enable Dependabot auto-PRs for all ecosystems. Block merge if any High or Critical CVE is unresolved. Weekly scheduled scan in CI. | #43 |

---

### Government / Compliance Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **GOV-01** | Tool is deployed against a GCC-High or DoD tenant using a commercial endpoint URI, causing auth failures or data-residency violations | H | C | 🔴 12 | Bobbie + Naomi | Connection attempt to `api.crm.dynamics.com` succeeds but returns data belonging to a GCC-High tenant via the wrong sovereignty boundary | Cloud-endpoint selection is explicit in environment profiles; default is never commercial. Pre-flight gate validates the resolved endpoint against the expected tenant cloud type before any data operation begins. | #41 #18 |
| **GOV-02** | Tool does not meet FedRAMP Moderate control requirements (AC, AU, IA controls) before a federal agency pilot | M | H | 🟠 6 | Bobbie | A federal customer or AO raises a FedRAMP or DISA STIG question during procurement or ATO review | Conduct a FedRAMP Moderate gap analysis spike (Bobbie). Map tool controls to AC-2, AC-3, AU-2, AU-9, IA-2, IA-5 before v1.0 GA. Document residual risk for operator acceptance. | #41 #42 |
| **GOV-03** | Migration of Dataverse data across cloud boundaries (commercial → GCC or GCC-High) creates unintended data-residency violations under DoD policy | M | C | 🔴 8 | Bobbie | Tool configuration allows source and destination tenants to resolve to different sovereign cloud regions | Cross-boundary migration is gated behind an explicit operator acknowledgement prompt and configuration flag (`allow-cross-boundary: true`). Default is blocked. | #41 #39 |
| **GOV-04** | CUI (Controlled Unclassified Information) present in migrated Dataverse records is not identified or protected during transit or at rest in checkpoint files | M | H | 🟠 6 | Bobbie | A government customer flags that specific entity types contain CUI; no classification tagging in the current scope model | Add a CUI-classification tagging capability to the migration scope model. Encrypt checkpoint files at rest. Bobbie drives a CUI-handling design spike pre-v1.0. | #25 #42 |

---

### Migration Correctness Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **MIG-01** | Record lookup (foreign-key) references migrate out of order, creating dangling or null references in the destination environment | H | H | 🔴 9 | Amos | Validation Engine reports FK violations after migration; spot-check of destination records shows null lookups | Dependency graph builder in the Migration Engine (#22) determines topological ordering of tables. Validation Engine asserts referential integrity post-migration. | #22 #23 #31 |
| **MIG-02** | Async Dataverse operations (plugin execution, calculated fields, rollup fields) produce inconsistent results when read back immediately after write | M | H | 🟠 6 | Amos + Naomi | Post-write validation reads stale or default values for fields that are updated by a plugin or async workflow | Add configurable settle-time delay after batch writes. Detect plugin-registered entities in metadata discovery and flag them. | #20 #23 |
| **MIG-03** | Resume after interruption replays already-committed records, causing duplicates or update-collision errors | M | H | 🟠 6 | Amos | Resume job completes but destination has duplicate records or unexpected field values vs. source | Checkpointing (#25) records per-record commit state. Upsert semantics (match on alternate key or GUID) prevent true duplicates. Prax writes a resume-after-failure regression test suite. | #25 #33 |
| **MIG-04** | Migration of system entities (Teams, Users, Business Units) that cannot be recreated in destination fails silently or leaves orphaned relationships | M | M | 🟡 4 | Amos + Naomi | Validation report shows records referencing non-existent system records in destination | Metadata discovery flags non-migratable system entity references. Pre-flight report surfaces these as warnings requiring operator action before migration runs. | #21 #22 |
| **MIG-05** | Full migration of tables with > 1 M rows exceeds operator-facing time expectations, causing tool to be perceived as non-viable for large tenants | M | H | 🟠 6 | Amos | A migration job against a 1 M-row table takes > 4 hours in lab testing | Incremental/delta migration strategy spike (#24). Parallelism in the execution planner. Progress reporting with ETA in the UI. Publish benchmark results in docs. | #24 #23 |

---

### Performance Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **PERF-01** | End-to-end migration wall-clock time exceeds operator SLA for a representative tenant (500 tables, 10 M rows) | M | H | 🟠 6 | Amos | Lab benchmark of representative tenant exceeds a 6-hour window | Parallelism tuning in the execution planner. Configurable concurrency and batch-size profiles. Delta migration reduces scope. Publish documented throughput benchmarks. | #22 #24 |
| **PERF-02** | Memory consumption of the migration process grows unboundedly when processing large pages or holding in-memory state for dependency resolution | M | M | 🟡 4 | Amos | Process memory exceeds 2 GB during a lab migration run; OOM crash observed | Use streaming / paging for all record reads. Dependency graph held as edge list, not full adjacency matrix. Memory profiling gate added to CI for large-table smoke tests. | #22 |
| **PERF-03** | UI (Code App + PCF) becomes unresponsive when the migration monitor receives high-frequency progress events from a large job | L | M | 🟢 2 | Alex | Browser tab freezes or React re-render spike shown in DevTools during a migration run with > 1 000 progress events/min | Throttle UI event subscription. Virtual-scroll progress list. Use a summary-refresh model rather than per-record events in the UI. | #35 #23 |

---

### OSS / Licensing Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **OSS-01** | A transitive dependency carries a copyleft (GPL/AGPL) license that is incompatible with the project's MIT license and commercial enterprise use | L | H | 🟡 3 | Drummer + Holden | License scanner (`dotnet-project-licenses`, `license-checker`) reports a GPL/AGPL transitive dependency in CI | License scanner runs in CI on every dependency change. Any copyleft finding blocks merge. Holden approves license policy exceptions. | #43 |
| **OSS-02** | The project's own MIT license is inconsistent with the use of proprietary Dataverse/Power Platform SDKs that carry Microsoft-specific terms | L | M | 🟢 2 | Holden + Devon | Legal question raised by an enterprise or government contributor during procurement | Review Microsoft SDK license terms. Document in README and CONTRIBUTING that the tool is MIT but depends on Microsoft-licensed SDKs. Escalate to Devon if a government AO raises concerns. | #1 |
| **OSS-03** | Community contributors introduce code with ambiguous IP provenance (no DCO / CLA) that creates downstream legal exposure | L | M | 🟢 2 | Devon + Monica | A PR arrives from an external contributor without a DCO sign-off | Add DCO or CLA bot to the repository. Document the requirement in CONTRIBUTING.md. | #47 |

---

### Delivery Risks

| ID | Risk | Likelihood | Impact | Score | Owner | Trigger | Mitigation | Issue |
|----|------|:---:|:---:|:---:|-------|---------|------------|-------|
| **DEL-01** | Single active contributor (Devon) creates a bus-factor-1 knowledge dependency; no handoff documentation exists | H | H | 🔴 9 | Devon + Monica | Devon unavailable for > 1 sprint; no other contributor can progress the migration engine | Monica's documentation deliverables (#47) include runbooks and architecture overviews. ADRs from Holden document every significant decision. Squad charters capture domain knowledge. | #47 #10 |
| **DEL-02** | Early scaffolding phase (v0.4) has no working CI/CD; manual validation creates quality regression risk before pipelines are stood up | H | M | 🟠 6 | Drummer | A breaking change is merged without CI catching it; no test run in > 3 days | CI/CD baseline (#43) is a v0.4 P0 item. Drummer has the highest priority on this work. No feature work merges to main without a passing CI run. | #43 |
| **DEL-03** | Scope creep from government-specific requirements (FedRAMP, IL4/IL5, CMK) expands v0.4 or v0.5 scope beyond team capacity | M | H | 🟠 6 | Holden + Devon | A government-specific requirement is accepted into a sub-v1.0 milestone without a scope-trade decision | Holden and Devon hold a scope gate at each sprint. Government requirements are parked in the v1.0 epic (#7) unless a concrete commitment requires earlier delivery. | #7 #41 |
| **DEL-04** | Power Platform SDK / `pac` CLI breaking changes between preview and GA releases break the scaffolding before product code is stable | M | M | 🟡 4 | Drummer + Naomi | A `pac` CLI update causes build or runtime failures in GitHub Actions | Pin `pac` CLI version in CI. Monitor Power Platform release blog. Integration tests run against a pinned CLI version. | #43 #18 |
| **DEL-05** | Lack of a versioned API contract between the Code App UI and the backend migration engine causes front-end/back-end integration drift | M | M | 🟡 4 | Holden + Alex | A UI screen fails to compile or render because a backend contract was changed without updating the TypeScript types | Application-layer contracts are the source of truth. TypeScript types are generated from C# contracts (or hand-maintained with a contract test). Contract tests (Prax) block merge if the seam breaks. | #31 #35 |

---

## High-Risk Summary (Score ≥ 8)

| ID | Risk (short) | Score | Linked Issue(s) |
|----|-------------|:-----:|-----------------|
| GOV-01 | Wrong cloud endpoint for GCC-High migration | 🔴 12 | #41 #18 |
| ARCH-01 | Infrastructure leaks into Domain layer | 🔴 9 | #11 #12 |
| API-01 | Dataverse throttling aborts migration mid-run | 🔴 9 | #18 #20 #22 |
| MIG-01 | FK ordering causes dangling references | 🔴 9 | #22 #23 #31 |
| DEL-01 | Bus-factor-1 knowledge risk | 🔴 9 | #47 #10 |
| SEC-01 | Credentials logged in plain text | 🔴 8 | #39 #40 #50 |
| GOV-03 | Cross-cloud-boundary data-residency violation | 🔴 8 | #41 #39 |

---

## Backlog of Follow-On Issues

The following risk mitigations require dedicated issues or are tracked under existing ones:

| Action | Owner | Suggested Issue / Epic |
|--------|-------|------------------------|
| ArchUnitNET / Roslyn layer-boundary rule in CI | Holden | #11 |
| Retry + throttle-handling in Dataverse Provider | Naomi | #18 |
| GCC-High endpoint and auth spike | Naomi + Bobbie | #41 |
| Credential redaction in logging framework | Bobbie | #40 #50 |
| PII scrub in checkpoint / report writers | Bobbie + Prax | #25 |
| Cross-boundary migration gate (config flag + prompt) | Bobbie | #41 |
| FedRAMP gap-analysis spike | Bobbie | #41 |
| Dependency graph + topological ordering for FK | Amos | #22 |
| Resume/idempotency regression test suite | Prax | #25 #33 |
| License scanner in CI | Drummer | #43 |
| DCO / CLA bot setup | Devon + Monica | #47 |
| Benchmark suite for large-table migrations | Amos | #24 |

---

## Review Log

| Date | Reviewer | Changes |
|------|----------|---------|
| 2026-07-31 | @copilot (initial) | Register created; all eight areas populated; high-risk summary and backlog added |

---

*This register is a living document. Update it at each sprint retrospective or when a new risk materialises. All risk mitigations must be linked to a GitHub issue before they are considered actionable.*
