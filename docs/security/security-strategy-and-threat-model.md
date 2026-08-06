# Security strategy and threat model

## Compliance posture

The Dataverse Migration Tool is **government-ready, not yet certified**. It is designed to be compatible with GCC High and other sovereign-cloud deployments by requiring configurable Entra authority hosts, tenant-specific Dataverse endpoints, secretless-by-default identity, auditable operations, and no public-cloud hardcoding.

This posture promises design compatibility and implementation guardrails for regulated environments. It does **not** claim FedRAMP, DoD, GCC High, or agency authorization. Certification evidence, controls inheritance, deployment boundary definitions, and authority-to-operate packages are future work and must be approved by a superseding decision or ADR before any formal compliance claim is made.

## Related standards

This strategy builds on, and does not replace, the [Dataverse authentication and secret-handling standard](auth-and-secret-handling.md). The [product vision](../product-vision.md), [testing strategy](../testing-strategy.md), and [CI/CD baseline strategy](../ci-cd-strategy.md) define the product, quality, and GitHub Actions terminology used here.

## Data flow and trust boundaries

```text
Operator browser / Code App
  -> API host / migration engine
  -> configuration provider
      -> non-secret config
      -> Key Vault reference or federated/managed identity binding
  -> Dataverse provider
      -> source Dataverse environment
      -> target Dataverse environment
  -> job/state store and operation log sink
  -> CI/CD build and release automation for code changes
```

Trust boundaries:

1. **Operator to Code App:** user interaction and delegated authorization begin in the browser. The Code App must not receive bearer tokens, refresh tokens, client secrets, or privileged migration credentials that are only needed server-side.
2. **Code App to API host:** API calls cross from presentation into the server-side control plane. Requests must be authenticated, authorized, validated, correlated, and logged without exposing tokens or PII.
3. **API host to identity platform:** token acquisition uses tenant-specific Entra authority and Dataverse scopes from the selected environment profile. Token cache partitioning must preserve source and target tenant isolation.
4. **API host to Dataverse:** source and target environments are separate security domains. The provider must resolve each Dataverse endpoint and scope from configuration, not static public-cloud assumptions.
5. **API host to configuration and secret backing services:** confidential credentials, when unavoidable, are referenced rather than stored inline. Entra workload identity federation or managed identity is preferred; Key Vault reference is the canonical fallback pattern.
6. **API host to state and logs:** migration state, reports, and audit events may contain sensitive operational metadata. Retention, access, encryption, and redaction policies must treat them as regulated records.
7. **Repository to CI/CD:** GitHub Actions validates changes without live tenant secrets by default. Any future deployment pipeline must use least-privilege permissions and federated identity or protected secret stores.

## Assets, actors, and boundaries

| Category | Examples | Protection goal |
| --- | --- | --- |
| Identities | operator account, service principal, managed identity, GitHub workflow identity | Authenticate strongly, authorize least privilege, isolate tenants and clouds |
| Tokens and credentials | access tokens, refresh tokens, device codes, certificates, confidential client credentials | Never persist or log; short-lived; stored only in approved caches or backing stores |
| Configuration | tenant IDs, environment URLs, cloud selection, authority hosts, scopes, Key Vault references | Validated, cloud-aware, non-secret unless explicitly backed by Key Vault |
| Migration data | source rows, target rows, metadata, solution components, attachments if supported | Minimize exposure; encrypt in transit and at rest; avoid browser/session leakage |
| Job state | checkpoints, run status, correlation IDs, retry metadata, validation reports | Durable, tamper-evident enough for audit, scoped to authorized operators |
| Logs and audit events | auth events, migration operations, errors, admin decisions | Complete enough for investigation; redacted by default |
| Build artifacts | packages, coverage reports, workflow logs, generated bundles | No secrets; provenance and dependency integrity maintained |

Primary actors:

- **Authorized operator:** administrator running validation or migration.
- **Tenant administrator:** grants app permissions, approves identity configuration, reviews audit records.
- **Migration service identity:** managed identity, federated workload identity, or fallback confidential app credential.
- **Dataverse and Entra platforms:** external trusted Microsoft services selected by cloud configuration.
- **Repository contributor:** submits code or documentation through pull requests.
- **Malicious insider or compromised account:** attempts data exfiltration, privilege escalation, or tampering.
- **External attacker:** probes public endpoints, poisoned dependencies, CI logs, or leaked credentials.

## Threat model by surface

### UI / Code App

| STRIDE | Abuse case | Required control |
| --- | --- | --- |
| Spoofing | Attacker impersonates an operator or reuses an abandoned browser session. | Authenticate every API call; require server-side authorization; expire sessions; use correlation IDs. |
| Tampering | Operator modifies source or target environment identifiers client-side. | Treat UI input as untrusted; resolve tenant, cloud, endpoints, and scopes server-side from validated configuration. |
| Repudiation | Operator denies starting validation, migration, cancellation, or rollback. | Audit user, action, source/target environment aliases, timestamp, and correlation ID. |
| Information disclosure | Code App bundle, browser logs, or client state exposes tokens, secrets, PII, or records. | Keep tokens and migration credentials server-side; redact errors; avoid storing sensitive payloads in client state. |
| Denial of service | Repeated UI actions create duplicate jobs or overload the API. | Use idempotency keys, request throttling, job state transitions, and server-side concurrency limits. |
| Elevation of privilege | UI enables an operator to run a migration outside their approved source or target scope. | Enforce authorization in the API and provider; never rely on disabled controls as a security boundary. |

### Dataverse Provider

| STRIDE | Abuse case | Required control |
| --- | --- | --- |
| Spoofing | Provider acquires a token for the wrong tenant or authority host. | Use per-environment tenant ID, cloud, authority host, and Dataverse resource; partition token caches accordingly. |
| Tampering | Endpoint or scope is hardcoded to public cloud or overwritten by untrusted input. | Resolve endpoints through `DataverseCloud` configuration and validated provider contracts. |
| Repudiation | Provider operations cannot be tied to a migration job or operator. | Emit operation events with job ID, environment alias, operation type, result, and correlation ID. |
| Information disclosure | Provider logs request headers, bearer tokens, SDK auth payloads, record data, or PII. | Apply redaction-by-default logging and never log token, secret, auth header, or raw row payload values. |
| Denial of service | Migration floods Dataverse APIs and triggers throttling or tenant disruption. | Respect Microsoft-supported throttling guidance, retries, backoff, batching limits, cancellation, and checkpointing. |
| Elevation of privilege | Overbroad app permissions allow migrations beyond intended tables or environments. | Use least-privilege scopes and Dataverse roles per source and target environment; validate permissions before execution. |

### Storage / job and state store

| STRIDE | Abuse case | Required control |
| --- | --- | --- |
| Spoofing | Unauthorized process reads or updates job checkpoints. | Require authenticated service access and least-privilege data-store permissions. |
| Tampering | Attacker changes checkpoint state to skip validation or replay writes. | Use validated state transitions, optimistic concurrency, and audit events for state changes. |
| Repudiation | No reliable history exists for job creation, resume, cancellation, or rollback. | Record append-oriented audit events for lifecycle changes and security-significant decisions. |
| Information disclosure | State contains copied records, PII, tokens, secrets, or connection strings. | Store only the minimum state needed for resume and reconciliation; encrypt at rest; redact sensitive values. |
| Denial of service | State store outage prevents resume or causes partial duplicate writes. | Design for durable checkpoints, safe retries, idempotency, and clear failed-state reporting. |
| Elevation of privilege | Shared state crosses tenants, clouds, or environments. | Partition by job, source environment, target environment, tenant, and cloud; enforce access checks on reads and writes. |

### Logs and audit

| STRIDE | Abuse case | Required control |
| --- | --- | --- |
| Spoofing | Fake log entries obscure who performed an action. | Emit logs from trusted server components and include authenticated principal and correlation ID when available. |
| Tampering | Logs are altered to hide unauthorized migration activity. | Send audit events to an approved sink with access controls and retention once implemented. |
| Repudiation | Authentication failures or migration changes are not auditable. | Audit authentication events and migration operations by default. |
| Information disclosure | Workflow logs, app logs, or exception details expose tokens, secrets, PII, or row data. | Redact by default; classify event fields; use safe error messages; prohibit raw payload logging. |
| Denial of service | Excessive logs hide security events or exhaust storage. | Apply structured log levels, sampling for noisy diagnostics, retention limits, and alertable audit events. |
| Elevation of privilege | Logs expose enough environment detail to help attackers pivot. | Limit infrastructure details; restrict log access; use aliases where possible for public artifacts. |

### CI/CD

| STRIDE | Abuse case | Required control |
| --- | --- | --- |
| Spoofing | Malicious workflow or forked PR obtains deployment identity. | Keep default PR CI secretless; use least-privilege `contents: read`; protect any deployment environment. |
| Tampering | Dependency, build script, or generated artifact is modified to exfiltrate credentials. | Use lockfiles, dependency review, vulnerability scanning, CodeQL/static analysis, and reviewed workflow changes. |
| Repudiation | Security-significant workflow changes are merged without traceability. | Require pull requests, branch protections, CODEOWNERS or security review for workflow/auth changes. |
| Information disclosure | Actions logs or artifacts expose tenant data, tokens, secret names, or PII. | Mask secrets, avoid live Dataverse in default CI, scan artifacts, and prohibit plaintext credentials in workflow files. |
| Denial of service | CI is abused for excessive runs or blocking required checks. | Use concurrency controls, scoped triggers, and required checks that are deterministic and tenant-secret-free. |
| Elevation of privilege | Workflow permissions allow unexpected repository write or cloud deployment. | Declare minimal permissions per job and prefer workload identity federation for future deployments. |

## Identity, token, and secret handling rules

The detailed implementation standard is [auth-and-secret-handling.md](auth-and-secret-handling.md). The strategy-level rules are:

1. **Least privilege:** request only the Dataverse scopes required for the selected operation and environment. Validate Dataverse privileges before migration execution.
2. **Tenant isolation:** source and target environment profiles must carry tenant ID, cloud, Dataverse resource, scopes, and authority host. Never reuse a token across tenant, cloud, or resource boundaries.
3. **Secretless preferred:** Entra workload identity federation or managed identity is the preferred default for automation and hosted services. Interactive or device-code delegated flows may be used only with a trusted prompt pattern for operator-driven scenarios.
4. **Key Vault reference fallback:** when a confidential credential is unavoidable, configuration stores only a Key Vault reference or approved secret reference. Plaintext credentials are never accepted in source, appsettings, tests, docs, logs, or `.squad` state.
5. **No sensitive logs:** tokens, refresh tokens, device codes, authorization headers, client secrets, certificates, passwords, connection strings, raw record payloads, and PII are redacted by default and must not be logged.
6. **Token lifetime:** access tokens are short-lived and refreshed through MSAL or the platform identity provider. Refresh tokens, if issued by a delegated flow, stay in the provider-managed cache only and are never exposed to application logs, job state, UI, or CI.
7. **Cache partitioning:** any token cache must include client ID, tenant ID, cloud, authority host, Dataverse resource, and account or service identity. Shared global mutable token state is prohibited.
8. **Cloud configurability:** commercial, GCC, GCC High, DoD, and future sovereign clouds must be selected by configuration and validated endpoint resolution, not by hardcoded public-cloud URLs.

## Secure defaults

- Default CI uses no live Dataverse tenant and no deployment secrets.
- Default configuration is non-secret and environment-specific.
- Authentication failures fail closed and produce redacted diagnostics.
- Migration execution runs server-side; the Code App remains a control plane.
- Source and target environments are modeled independently, including tenant and cloud.
- Job creation and execution require explicit validated source, target, scope, and operator context.
- Logs are structured, redacted, and correlation-friendly by default.
- Public artifacts, fixtures, and docs use placeholders and aliases rather than real tenant, user, or record data.
- Sovereign endpoints and authority hosts are configurable and testable.

## Audit needs

Authentication events and migration operations are auditable by default.

Minimum authentication audit events:

- token acquisition attempt and result category, without token material;
- tenant, cloud, environment alias, authority host category, and client application identifier;
- trusted prompt required, prompt completed, prompt rejected, or prompt timed out;
- permission validation result;
- authentication or authorization failure category.

Minimum migration audit events:

- validation started, completed, failed, or cancelled;
- migration job created, started, paused, resumed, completed, failed, cancelled, or rollback/compensation requested;
- source and target environment aliases, tenant/cloud categories, selected component or table scope, and correlation ID;
- batch/checkpoint progress and retry/failure categories;
- configuration changes that affect identity, endpoint selection, logging, retention, or storage.

Retention and redaction expectations:

- Audit retention must satisfy the deployment owner policy; until a deployment-specific policy exists, choose the shortest retention that supports troubleshooting and compliance review.
- Security audit events should go to a protected sink once implemented; ordinary diagnostic logs are not a substitute for audit records.
- Logs and reports must redact tokens, secrets, auth headers, device codes, connection strings, raw row payloads, and PII by default.
- Public CI artifacts must not include live environment identifiers, tenant-specific data, or confidential configuration.

## Security gates

Sensitive work is gated by Bobbie/security review when it changes any of the following:

- authentication, authorization, token acquisition, token caching, prompt flows, or scopes;
- secret/reference handling, Key Vault integration, workload identity federation, managed identity, or GitHub Actions identity;
- Dataverse endpoint or cloud authority resolution;
- storage of job state, migration records, validation reports, or audit events;
- logging, telemetry, error handling, redaction, or retention;
- CI/CD workflows, workflow permissions, dependency scanning, secret scanning, deployment automation, or release artifacts;
- any new surface that processes credentials, PII, tenant identifiers, or migration data.

Security review must confirm:

1. the threat model is updated for new surfaces or changed trust boundaries;
2. no tokens, secrets, auth headers, connection strings, raw PII, or raw migration data are logged or committed;
3. least-privilege scopes and Dataverse permissions are documented and validated;
4. tenant and sovereign-cloud endpoint selection remain configurable;
5. secretless identity is used by default, with Key Vault reference fallback only when justified;
6. audit events cover authentication and migration operations without leaking sensitive values;
7. CI remains secretless by default and any privileged workflow uses protected environments and least privilege.

## Follow-up security backlog

Decision-ready items, not implemented by this spike:

1. Wire Key Vault reference resolution for confidential credential fallback while preserving secretless as the preferred path.
2. Configure Entra workload identity federation or managed identity for hosted execution and future deployment pipelines.
3. Implement a protected audit-log sink with retention, access control, and export guidance for regulated deployments.
4. Add CI secret scanning, dependency vulnerability scanning, dependency review, and CodeQL/static analysis with documented thresholds.
5. Add permission preflight checks for source and target Dataverse roles and scopes before validation or migration execution.
6. Define deployment-specific audit retention profiles for commercial, GCC, GCC High, and other sovereign environments.
7. Add security regression tests for redaction, endpoint resolution, token cache partitioning, and plaintext-secret rejection.
