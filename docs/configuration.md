# Configuration guide

Use this guide to define source, target, security, and observability settings before you run a migration.

## Configuration areas

Plan configuration in these areas:

- Source environment connection
- Target environment connection
- Authentication method
- Migration scope and include or exclude rules
- Checkpoint storage for resume operations
- Log and validation report retention

## Recommended configuration model

Keep configuration external to the application and version only sanitized examples.

| Area | Recommendation |
| --- | --- |
| Environment URLs | Store as environment-specific settings |
| Credentials | Use managed identities, service principals, or secure secret stores |
| Migration scope | Track in reviewed configuration files or deployment parameters |
| Checkpoints | Store in durable storage with access control and backup |
| Logs and reports | Send to enterprise-approved monitoring or storage targets |

## Identity and access

Use least privilege for every migration path.

- Separate contributor access from operator access.
- Prefer non-interactive identities for repeatable automation.
- Limit source read and target write permissions to the minimum required scope.
- Review admin-consent requirements before first use.

## Environment readiness

Confirm these settings before execution:

- Source and target environments are reachable.
- Required tables, solution components, and dependencies are discoverable.
- API limits, service protection limits, and maintenance windows are understood.
- Backup and restore paths are approved for the selected environments.

## Government and regulated workloads

When you work in government or highly regulated environments:

- Document the cloud instance for both source and target.
- Avoid configurations that depend on public-cloud-only services.
- Capture authority-to-operate or enclave restrictions in the change record.
- Ensure operators know where audit evidence must be stored after the run.

## Configuration review checklist

Before a migration starts, review:

1. Source and target identifiers
2. Auth method and approval status
3. Migration scope
4. Resume checkpoint location
5. Validation report destination
6. Rollback data and recovery owners

## Related documentation

- [Installation guide](installation.md)
- [Rollback guide](runbooks/rollback-guide.md)
