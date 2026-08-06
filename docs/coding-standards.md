# Coding standards

These standards keep the Dataverse Migration Tool easy to review, safe for public OSS work, and aligned with [ADR-0002: Clean Architecture Boundaries](adr/ADR-0002-clean-architecture-boundaries.md). When these standards conflict with local preference, follow the accepted ADR and update or supersede the ADR before changing the architecture.

## Naming and organization

- Use clear, domain language: migration plan, validation result, checkpoint, provider, environment profile, and operation log should mean the same thing in code, docs, and tests.
- Name types by responsibility, not implementation detail. Prefer `MigrationPlan`, `ValidationSummary`, and `IDataverseProvider` over names tied to a transport, SDK, or database.
- Keep files close to the layer that owns the concept. Do not create cross-layer utility buckets that hide dependency direction.
- Use `PascalCase` for C# types, records, methods, and public properties; `camelCase` for local variables and parameters; `Async` suffixes for asynchronous methods that return `Task` or `ValueTask`.
- Use `PascalCase` for React components and TypeScript types/interfaces; `camelCase` for functions, variables, props, hooks, and module-level constants unless a platform API requires another shape.
- Prefix React hooks with `use` and keep hook files focused on reusable state or side effects, not rendering.

## Layering and dependency rules

ADR-0002 is authoritative: dependencies point inward.

```text
Presentation / Code App -> API host -> Application -> Domain
API host -> Infrastructure -> Application -> Domain
```

- **Domain** owns entities, value objects, enums, invariants, and business rules. It must have zero references to Dataverse SDK packages, ASP.NET Core, Fluent UI, React, storage providers, HTTP clients, logging sinks, or infrastructure packages.
- **Application** owns use cases, orchestration contracts, ports, request/result models, and workflow coordination. It depends on Domain only.
- **Infrastructure** owns Dataverse adapters, identity/token implementations, configuration providers, persistence, validation adapters, logging sinks, and other external systems. It implements Application ports.
- **API host** owns HTTP concerns, authentication/authorization when introduced, request validation, status-code mapping, and dependency injection composition.
- **Code App (`src\app`)** is the operator control plane. It can render migration and validation workflows, but it must not perform bulk migration execution or own Dataverse provider logic.
- Add or update an ADR before changing these boundaries. Domain purity is merge-blocking.

## C# backend standards

- Target .NET 9 and use the repository defaults in `src\backend\Directory.Build.props`: nullable reference types enabled, implicit usings enabled, warnings as errors, and latest C# language version.
- Treat nullable warnings as design feedback. Use non-nullable types for required data, nullable types for genuinely optional values, and guard inputs at layer boundaries.
- Prefer records or small immutable types for request/result models and value objects. Keep entities responsible for enforcing their own invariants.
- Use async/await for I/O and external calls. Accept and propagate `CancellationToken` through Application, Infrastructure, and API paths that can be cancelled.
- Define ports in Application before Infrastructure implements them. Do not let Infrastructure types leak into Domain or Application contracts.
- Keep dependency injection registrations in the API composition root or Infrastructure extension methods called from it. Avoid service locator patterns and static mutable dependencies.
- Prefer constructor injection. Keep constructors small and require only the collaborators the type actually needs.
- Map expected validation or business failures to explicit result types where possible; reserve exceptions for exceptional or unrecoverable conditions.
- For API endpoints, return consistent problem details for invalid input, unauthorized access, not-found resources, conflicts, and unexpected failures.

## TypeScript and React standards

- The Code App uses React, Vite, TypeScript, and Fluent UI v9 from `src\app`. Build UI with Fluent UI v9 components and tokens before introducing custom controls.
- Keep components small and operator-focused. Container components may coordinate state and API calls; presentational components should receive typed props and render predictable UI.
- Prefer explicit prop types and discriminated unions for UI states such as loading, ready, empty, validation failed, migration running, and error.
- Keep hooks side-effect focused and testable. Hooks that call APIs should depend on typed client functions rather than constructing requests inline across components.
- Do not put secrets, bearer tokens, tenant-specific sensitive values, or raw connection strings in browser state, logs, fixtures, or committed configuration.
- Use accessible labels, button text, and status messages. Migration and validation flows must be usable without relying only on color.
- Keep API DTOs and TypeScript models aligned with backend contracts; add contract tests when shared shapes become stable.

## Error handling

- Validate inputs at the boundary closest to the caller: UI form validation for operator input, API model validation for HTTP requests, Application validation for use-case rules, and Domain guards for invariants.
- Prefer actionable errors that tell an operator what failed and what can be retried, corrected, or escalated.
- Preserve cause chains in logs and diagnostics without exposing secrets or personal data.
- Use retries only around transient external dependencies and keep retry policy in Infrastructure, not Domain.
- Do not swallow cancellation; cancelled operations should leave durable state clear enough to resume, retry, or report cancellation.

## Logging and telemetry

Follow [Dataverse authentication and secret-handling standard](security/auth-and-secret-handling.md): never log client secrets, certificates, refresh tokens, access tokens, device codes, passwords, connection strings, bearer tokens, authorization headers, or MSAL result payloads.

- Treat tenant IDs, environment IDs, user identifiers, and record data as sensitive operational data. Log the minimum needed for support and redact or hash where practical.
- Do not log PII, migration payload contents, Dataverse row values, attachments, or solution exports unless a future ADR explicitly approves a redacted diagnostic path.
- Log stable operation IDs, job IDs, environment profile names, cloud names, component counts, durations, retry counts, and high-level status transitions.
- Put logging sinks and provider-specific telemetry adapters in Infrastructure. Domain should not depend on logging frameworks.

## Tests

Use [Testing strategy and quality gates](testing-strategy.md) as the source of truth.

- Backend tests use xUnit. Unit-test Domain invariants and Application orchestration with deterministic fakes before adding integration coverage.
- Infrastructure tests may exercise adapters against local fakes, mocks, or containers, but default CI must not require a live Dataverse environment or tenant secrets.
- API tests should cover route contracts, validation errors, auth/authorization behavior when introduced, status mapping, and problem details.
- Code App tests should use Vitest, React Testing Library, and user-event once wired. Mock backend boundaries by default.
- Keep test data deterministic. Redact tenant/environment identifiers and never commit secrets in fixtures.
- Add tests with behavior changes. For docs-only changes, tests are not required unless documentation tooling exists.

## Formatting and local quality gates

- Backend formatting follows the .NET SDK conventions for the solution. Run `dotnet build src\backend` and `dotnet test src\backend` before opening backend PRs.
- Frontend formatting follows `src\app\.prettierrc` and the flat ESLint config in `src\app\eslint.config.js`.
- In `src\app`, use `npm ci`, `npm run lint`, `npm run typecheck --if-present`, `npm run format:check`, and `npm run build` for local validation.
- Keep generated artifacts, build outputs, secrets, and local environment files out of source control.
