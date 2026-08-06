# Testing strategy and quality gates

## Goals

The Dataverse Migration Tool must prove migration correctness before it proves throughput. Tests should be deterministic, fast by default, and safe for public and sovereign cloud contributors. CI must not require a live Dataverse environment or tenant secrets.

## Test pyramid

### .NET backend

| Layer | Tooling | What belongs here | What does not belong here |
| --- | --- | --- | --- |
| Domain | xUnit unit tests | Entity state transitions, value-object invariants, enum-driven behavior, validation result aggregation rules, component-selection semantics. | Dataverse SDK calls, persistence, HTTP, file system, clocks unless injected. |
| Application | xUnit unit tests with in-memory/fake ports | Use-case orchestration, request/response contracts, port behavior expectations, cancellation propagation, validation and migration workflow sequencing. | Real Dataverse, database-specific behavior, API serialization. |
| Infrastructure | xUnit integration tests with local fakes/containers where needed | Dataverse provider adapters behind mocked SDK seams, job-store implementations, logging adapters, serialization, retry/checkpoint persistence. | Live Dataverse in default CI. |
| API | xUnit/WebApplicationFactory integration tests | Route contracts, model binding, validation errors, auth/authorization behavior when introduced, HTTP status mapping, problem details. | Browser automation and Code App interaction. |

Unit tests should be the majority of the suite and should run in seconds. Integration tests should exercise real project seams but still be hermetic. Any test requiring network, tenant credentials, or a live Dataverse environment must be opt-in and excluded from default CI.

### TypeScript/React Code App

Use Vitest with React Testing Library for unit and component tests. Keep Vite, ESLint, TypeScript, and Prettier as the baseline developer workflow.

| Layer | Tooling | What belongs here |
| --- | --- | --- |
| Pure TypeScript units | Vitest | Data shaping, validation helpers, API client request construction, error mapping, feature flags. |
| React component tests | Vitest + React Testing Library + user-event | Fluent UI rendering, operator flows, accessible labels, disabled/loading/error states, form validation, interaction with mocked API clients. |
| Contract tests | Vitest or xUnit-generated OpenAPI/schema checks | Code App expectations for backend route shapes, DTO compatibility, and error payloads. |
| E2E smoke tests | Playwright, later | One or two critical operator journeys against a local/test-hosted API with mocked Dataverse responses. |

Do not add live Dataverse dependencies to Code App tests. UI tests should mock the backend API boundary unless explicitly running an opt-in E2E environment.

## Integration, contract, and E2E boundaries

- **Unit tests** isolate one class, record, component, hook, or pure function and replace collaborators with fakes.
- **Integration tests** cross a real boundary inside this repository, such as Application plus Infrastructure job store, API plus dependency injection, or a Dataverse provider against a mocked SDK seam. They may use local files, in-memory stores, or containers if deterministic.
- **Contract tests** verify producer/consumer compatibility without running full workflows. Backend API contracts should cover status codes, DTO fields, error shapes, and versioning expectations. Dataverse adapter contracts should codify the minimal SDK behavior the migration engine relies on.
- **E2E tests** validate user-visible flows from Code App to API to mocked migration services. Keep these few, stable, and focused on release confidence rather than exhaustive coverage.
- **Live Dataverse tests** are a separate manual or scheduled opt-in suite. They must require explicit environment variables, use non-production environments, create uniquely named disposable data, and clean up after themselves.

## Dataverse mocking and fixtures

Default CI must use no live Dataverse environment. The test strategy is:

1. Define narrow Dataverse adapter/port interfaces in Application and test most migration logic against deterministic fakes.
2. Keep canonical fixtures for tables, columns, relationships, choices, users/teams, solution components, and sample records. Fixtures should include edge cases such as missing lookups, alternate keys, owner/team references, many-to-many relationships, and unsupported component types.
3. Build reusable fake providers that can simulate paging, throttling, partial failures, duplicate keys, relationship ordering, checkpoint interruption, and rollback failures.
4. Keep generated IDs, timestamps, and ordering deterministic in tests.
5. Redact tenant/environment identifiers from committed fixtures and never require secrets in CI.
6. Add opt-in live Dataverse smoke tests only after Drummer has a safe secret and environment story; these tests must not block public PR validation.

## Critical paths that must be covered

The coverage floor does not replace mandatory scenario coverage. Before a release, these paths must have explicit automated tests:

- Migration plan creation from selected tables, solutions, and data scopes.
- Migration correctness for rows, columns, lookups, owners, teams, choices, attachments/notes if supported, and many-to-many relationships.
- Dependency ordering so parent records and solution components are available before dependents.
- Relationship rehydration and alternate-key matching across environments.
- Validation reports for missing metadata, missing privileges, unsupported components, duplicate keys, and environment/cloud mismatches.
- Resume/checkpoint behavior after interruption, including idempotent retries and no duplicate target records.
- Rollback/compensation behavior for failed migrations, including partial rollback and rollback failure reporting.
- Cancellation, pause, failure, and completed job status transitions.
- API error mapping for validation failures, not-found jobs, unauthorized requests when auth lands, and unexpected exceptions.
- Code App operator flows for selecting environments/components, starting validation, reviewing validation results, starting migration, and seeing progress/failure states.

## Coverage expectations

Initial project floor: **80% line coverage for the combined backend test projects and 75% line coverage for the Code App once Vitest is wired**. Release-critical migration, validation, checkpoint/resume, and rollback code should target **90%+ line coverage and meaningful branch coverage**. Generated code, DTO-only records, and framework startup glue may be excluded when they do not contain behavior.

Coverage reports should be produced in CI, uploaded as artifacts, and fail the build when the floor is missed. Any coverage exemption should be documented in the PR that introduces it.

## CI quality gates for Drummer to wire

Prax defines these gates; Drummer owns pipeline implementation.

### Required on every pull request

1. `dotnet restore src\backend`
2. `dotnet build src\backend --no-restore --configuration Release`
3. `dotnet test src\backend --no-build --configuration Release --collect:"XPlat Code Coverage"`
4. Backend coverage threshold: fail below 80% line coverage once coverlet report processing is wired.
5. `npm ci` in `src\app`
6. `npm run typecheck --if-present` in `src\app`
7. `npm run lint` in `src\app`
8. `npm run format:check` in `src\app`
9. `npm test -- --coverage --run` in `src\app` after Vitest and React Testing Library are added.
10. Code App coverage threshold: fail below 75% line coverage once Vitest exists.

### Required before release

- Full backend unit and integration suite.
- Code App unit/component suite.
- API contract tests.
- E2E smoke suite against local API and mocked Dataverse services.
- Opt-in live Dataverse smoke validation only when safe non-production credentials are available.
- Published coverage and test-result artifacts.

### Non-goals for this issue

This document defines the gates and strategy. It does not add CI workflow plumbing, Vitest dependencies, Playwright setup, or live Dataverse environments.
