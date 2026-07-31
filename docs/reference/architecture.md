# Architecture reference

Use this page to understand the intended system shape and trust boundaries for dataverse-migration-tool.

## Design goals

The tool is designed to be:

- Secure by default
- Resumable across long-running migrations
- Useful for both enterprise and government environments
- Explicit about validation and rollback

## Planned components

| Component | Responsibility |
| --- | --- |
| Code App or operator UX | Scope selection, progress visibility, validation review, and rollback guidance |
| Migration engine | Ordering, batching, checkpointing, resume, and error handling |
| Dataverse client layer | Access to tables, metadata, and solution component APIs |
| Validation layer | Counts, dependency checks, and migration outcome reporting |
| Configuration and secret sources | Externalized environment settings and secure credential access |

## Data flow

1. The operator selects source, target, and migration scope.
2. The tool validates connectivity, permissions, and dependencies.
3. The migration engine reads metadata and data from the source environment.
4. The engine writes to the target in ordered stages and persists checkpoints.
5. The validation layer compares expected and actual outcomes.
6. The operator either signs off, resumes, or rolls back.

## Trust boundaries

Treat these as separate trust zones:

- Operator workstation or build agent
- Source Power Platform environment
- Target Power Platform environment
- Secret storage and identity platform
- Log, report, and checkpoint storage

Do not assume data, identities, or logs can move freely between those boundaries, especially in GCC High or DoD scenarios.

## Enterprise and government considerations

- Keep secrets and environment configuration outside the application package.
- Expect conditional access, privileged identity management, and approval workflows.
- Design for cloud-instance awareness so public and government endpoints are not mixed.
- Keep audit artifacts in storage approved for the target workload.

## Related documentation

- [Configuration guide](../configuration.md)
- [API reference](api-reference.md)
