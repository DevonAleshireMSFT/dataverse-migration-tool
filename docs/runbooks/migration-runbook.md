# Migration runbook

Use this runbook to plan and execute a migration from preflight through validation and resume.

> ℹ️ This runbook documents the expected operator workflow for v1. Update implementation-specific commands as the application surfaces are added.

## Roles

Use clear ownership before the run starts:

- Migration operator: runs the migration and captures evidence
- Environment owner: approves source and target scope
- Security or compliance reviewer: confirms policy alignment for enterprise or government workloads
- Business validator: signs off on post-migration validation

## Preflight

Complete preflight before you touch the target environment:

1. Confirm the change window and success criteria.
2. Confirm source and target environment access.
3. Export or back up the source of truth required for rollback.
4. Validate that required solution dependencies exist in the target.
5. Confirm checkpoint storage, log retention, and validation report destinations.
6. Record the migration scope, operator identity, and environment IDs.

## Migration

Run the migration in controlled phases:

1. Authenticate to the source and target environments.
2. Load the reviewed migration scope.
3. Start with solution components and shared dependencies.
4. Migrate data in dependency-safe batches.
5. Capture checkpoints after each successful stage.
6. Stop immediately if the tool reports authorization, dependency, or data-integrity failures that affect correctness.

## Validation

Validate the result before you declare success:

1. Review the migration summary and any skipped items.
2. Compare record counts and key business identifiers.
3. Confirm solution components imported without unresolved dependencies.
4. Review warnings separately from blocking errors.
5. Get sign-off from the business validator and environment owner.

## Resume

Use resume only when the checkpoint state is trustworthy:

1. Review the failed stage and confirm the root cause is resolved.
2. Confirm the target environment is still in a known state.
3. Rehydrate the last successful checkpoint.
4. Resume from the failed stage instead of replaying the entire migration.
5. Re-run validation for resumed scope and adjacent dependencies.

Resume is appropriate when:

- A transient connectivity issue interrupted the run
- Service protection limits delayed completion
- An operator intentionally paused after a clean checkpoint

Resume is not appropriate when:

- The checkpoint is incomplete or corrupted
- The target environment was manually changed after failure
- The rollback decision has already been approved

## Escalation points

Escalate before continuing when:

- Source and target metadata do not match the expected scope
- You detect partial writes with uncertain integrity
- Compliance boundaries change during the change window
- Business validation finds materially incorrect data

## Related documentation

- [Troubleshooting guide](troubleshooting.md)
- [Rollback guide](rollback-guide.md)
