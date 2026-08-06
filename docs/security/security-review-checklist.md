# Security review checklist

This focused checklist records the security Definition of Done checkpoints for issue #39. It should be folded into the formal Definition of Done when issue #14 lands; this file intentionally does not create the full DoD.

Use this checklist for any change that touches authentication, authorization, Dataverse connectivity, configuration, secrets, job/state storage, logs, CI/CD, audit, or migration data handling.

## Required security checkpoints

- [ ] Threat model updated for any new surface, data flow, trust boundary, identity flow, storage location, log sink, or CI/CD path.
- [ ] Secret scan is clean for source, docs, workflows, generated artifacts, and staged changes.
- [ ] No access tokens, refresh tokens, device codes, authorization headers, client secrets, certificates, passwords, connection strings, raw PII, or raw migration payloads are logged, persisted in job state, included in browser state, or committed.
- [ ] Least-privilege Dataverse scopes and roles are documented and verified for the source and target environments.
- [ ] Source and target tenant, cloud, authority host, Dataverse resource, and scopes remain independently configurable and validated.
- [ ] Sovereign-cloud and GCC High endpoint selection is configurable; no public-cloud endpoint or Entra authority is hardcoded into product logic.
- [ ] Entra workload identity federation or managed identity is used where hosted automation is possible; Key Vault reference is used for any justified confidential credential fallback.
- [ ] Token cache partitioning preserves client, tenant, cloud, authority host, Dataverse resource, and account/service identity boundaries.
- [ ] Authentication events and migration operations emit auditable, structured, redacted events with correlation IDs.
- [ ] CI/CD changes use least-privilege workflow permissions, remain secretless by default, and protect any future deployment identity behind environment approvals.
- [ ] Security-sensitive changes have Bobbie/security review before merge.

## Blocking conditions

A change must not merge if it introduces plaintext secrets, token logging, unredacted PII in logs, public-cloud-only endpoint assumptions, unaudited migration execution, overbroad workflow permissions, or a new credential-bearing surface that is missing from the threat model.
