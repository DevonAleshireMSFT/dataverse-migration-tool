# Rollback guide

Use this guide when a migration must be contained and reversed.

## Rollback principles

Follow these principles:

- Stop additional writes before you start rollback.
- Use approved backups, exports, or restore points as the recovery source.
- Keep the operator, environment owner, and business validator aligned on the rollback decision.
- Preserve audit evidence from both the failed migration and the rollback operation.

## Rollback triggers

Initiate rollback when:

- Validation shows business-critical data corruption or loss.
- A dependency failure leaves the target in an unusable state.
- Security, compliance, or boundary conditions change during execution.
- Resume is unsafe because the checkpoint or target state is untrusted.

## Rollback procedure

1. Declare the migration failed and freeze further changes.
2. Capture logs, checkpoints, and validation evidence.
3. Restore the target environment from the approved backup or restore path.
4. Re-apply required baseline solution components if the restore process does not include them.
5. Validate the restored environment against the pre-migration baseline.
6. Record the rollback outcome and residual issues in the change record.

## Post-rollback review

Before you schedule another migration:

- Confirm the root cause is understood.
- Identify what preflight control failed to prevent the issue.
- Update the runbook, validation process, or configuration baseline as needed.
- Obtain a new approval for the next migration window.

## Government and enterprise audit notes

For regulated environments, ensure rollback artifacts are retained in approved locations and that incident or change records contain:

- The reason rollback was triggered
- The approving authority
- The restore source and timestamp
- Evidence that validation completed after restore

## Related documentation

- [Migration runbook](migration-runbook.md)
- [Troubleshooting guide](troubleshooting.md)
