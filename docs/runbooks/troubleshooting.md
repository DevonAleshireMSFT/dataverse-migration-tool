# Troubleshooting guide

Use this guide when a migration does not complete as expected.

## Triage order

Work in this order:

1. Confirm the exact stage that failed.
2. Capture the error message, correlation ID, and timestamp.
3. Determine whether the failure is transient, environmental, permission-related, or data-related.
4. Decide whether to resume, retry from the start, or roll back.

## Common failure scenarios

| Scenario | What to check | Recovery path |
| --- | --- | --- |
| Authentication failure | Tenant, cloud endpoint, consent, conditional access | Correct the identity issue, then restart preflight or resume if no writes occurred |
| Missing dependency | Solution component order, target prerequisites | Add the missing dependency, then resume from the blocked stage |
| Service protection or throttling | API volume, batch size, maintenance window | Pause, wait for limits to reset, then resume from the last clean checkpoint |
| Validation mismatch | Record counts, alternate keys, mapping rules | Investigate the affected scope before approving retry or rollback |
| Corrupted checkpoint | Storage durability, serialization, manual edits | Discard the checkpoint, restore the target if required, and restart with a new change record |

## When to resume

Resume when:

- The failure was transient
- The last checkpoint completed cleanly
- No manual changes were made to the target after failure

## When to roll back

Roll back when:

- Data integrity is in doubt
- Validation finds business-critical mismatches
- The target environment is no longer trustworthy

## Evidence to collect

Capture and retain:

- Migration logs
- Validation reports
- Checkpoint identifiers
- Environment and tenant identifiers
- Operator notes describing the decision to retry, resume, or roll back

## Related documentation

- [Migration runbook](migration-runbook.md)
- [Rollback guide](rollback-guide.md)
