# Contributing to dataverse-migration-tool

Thanks for helping improve this project. This guide defines coding standards and contribution conventions for OSS contributors.

## Architecture boundaries (clean layering)

Keep dependencies pointed inward so domain and migration logic stay portable and testable.

- **Domain/Application layer**: business rules, migration planning, validation decisions.
  - Must not depend on UI frameworks, Dataverse transport SDK details, or CI tooling.
- **Infrastructure layer**: Dataverse Web API clients, `pac` CLI integrations, storage/checkpoint adapters.
  - Implements interfaces defined by the application layer.
- **Presentation layer**: TypeScript/React/Fluent UI and any CLI/API entry points.
  - Orchestrates use-cases; does not embed migration business rules.

Dependency rule: `Presentation -> Application -> Domain`, and `Infrastructure -> Application/Domain contracts` only.

## Naming and organization

- Use clear, intent-revealing names (`MigrationPlan`, `CheckpointStore`, `EnvironmentCompareView`).
- Keep files and folders aligned to domain concepts (data migration, solution components, validation, rollback).
- Avoid generic helpers unless shared by multiple features with a single responsibility.

## Language-specific standards

### C# (.NET)

- Enable nullable reference types and treat nullability warnings as design feedback.
- Use async I/O APIs end-to-end (`async`/`await`), avoid blocking calls (`.Result`, `.Wait()`).
- Inject dependencies through interfaces and constructors; avoid hidden static state.
- Return structured results/errors instead of throwing for expected validation failures.
- Prefer immutable request/response contracts where practical (`record`/readonly properties).

### TypeScript/React

- Use strict typing; avoid `any` unless explicitly justified and isolated.
- Keep components focused: UI composition in components, business logic in hooks/services.
- Model API/data contracts with explicit types and validate unknown external inputs.
- Prefer controlled state flows and predictable effects (`useEffect` with explicit dependencies).
- Keep Fluent UI components accessible (labels, keyboard navigation, semantic roles).

## Error handling and logging

- Fail fast on invalid configuration and missing required inputs.
- Include actionable context in errors (entity/schema name, migration phase, environment id) without leaking secrets.
- Use structured logs with consistent fields (correlation id, operation, duration, outcome).
- Log at appropriate levels:
  - `Debug`: detailed diagnostics for troubleshooting
  - `Information`: normal migration milestones
  - `Warning`: recoverable issues/retries
  - `Error`: failed operations requiring attention

## Testing expectations

- Add or update tests with each behavior change.
- Prioritize fast unit tests for migration planning, dependency ordering, and validation decisions.
- Add integration tests for Dataverse API boundaries and resumable checkpoint flows when behavior crosses process boundaries.
- Include edge cases: partial failure, retry semantics, idempotency, and rollback guidance generation.

## Formatting and quality

- Run repository lint/build/test commands before opening a PR.
- Keep PRs focused and small; separate refactors from behavior changes when possible.
- Do not include unrelated formatting-only churn in feature/bugfix PRs.

## Contribution workflow (OSS)

1. Open an issue (or confirm an existing one) before major changes.
2. Create a branch from `main` with a descriptive name.
3. Implement the smallest complete change that satisfies acceptance criteria.
4. Update tests and documentation for affected behavior.
5. Verify lint/build/tests locally.
6. Open a PR with:
   - problem statement and scope
   - summary of approach
   - validation evidence (tests/checks run)
   - risks or follow-up items
7. Address review feedback with focused follow-up commits.

## Security and secrets

- Never commit credentials, tokens, connection strings, or environment secrets.
- Use secure defaults for auth flows and data access boundaries.
- Sanitize and validate external inputs at system boundaries.

