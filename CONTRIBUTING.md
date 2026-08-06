# Contributing

Thank you for helping improve the Dataverse Migration Tool. This is a public MIT-licensed project, and contributions should be welcoming, reviewable, and safe for contributors who do not have access to a Dataverse tenant.

## Before you start

- Look for an existing GitHub issue or open one to discuss meaningful changes before investing significant time.
- Confirm the issue has enough context to be ready. Use the [Definition of Ready and Definition of Done](docs/definition-of-ready-and-done.md) for readiness and completion expectations. That document may land through sibling PR #14, but this path is the intended reference.
- Read the architecture expectations in [ADR-0002](docs/adr/ADR-0002-clean-architecture-boundaries.md) and the [coding standards](docs/coding-standards.md).
- Never commit secrets, tokens, connection strings, tenant credentials, or sensitive Dataverse data.

## Branch naming

Use descriptive branches tied to an issue when possible:

- Squad-authored work: `squad/{issue-number}-{kebab-case-slug}`
- Copilot-authored work: `copilot/{issue-number}-{kebab-case-slug}` or the platform-provided `copilot/*` branch name
- Human contributor work: any clear branch name is fine, but `{issue-number}-{kebab-case-slug}` is preferred

Examples:

```text
squad/16-coding-standards
copilot/42-fix-validation-result-mapping
42-add-checkpoint-resume-tests
```

## Commit style

- Use short, imperative commit subjects such as `docs: publish coding standards` or `test: cover migration plan validation`.
- Prefer conventional prefixes when they help reviewers: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`.
- Keep commits focused. Do not mix unrelated formatting, documentation, and behavior changes.
- Include issue references in commits or PR descriptions when useful.

## Pull request expectations

Every PR should:

- Link the issue it resolves or advances, for example `Closes #16`.
- Explain what changed, why it changed, and how it was validated.
- Keep CI green and include any relevant local command output in the PR description.
- Update docs, ADRs, examples, and tests when behavior, architecture, commands, or contributor workflow changes.
- Preserve clean architecture boundaries: Domain has no SDK, UI, hosting, storage, or Infrastructure references, and dependencies point inward.
- Keep PRs reasonably small. If a change spans architecture, backend, frontend, tests, and docs, consider splitting it.

## Local validation

Run the smallest commands that cover the files you changed.

### Backend (.NET 9)

From the repository root:

```powershell
dotnet restore src\backend
dotnet build src\backend --configuration Release
dotnet test src\backend --configuration Release
```

For quick iteration, `dotnet build src\backend` and `dotnet test src\backend` are acceptable before a final Release validation.

### Frontend Code App

From `src\app`:

```powershell
npm ci
npm run lint
npm run typecheck --if-present
npm run format:check
npm run build
```

If frontend tests are added or changed, also run the test command defined in `src\app\package.json`.

### Documentation-only changes

Docs-only PRs do not need build or test runs unless they change documented commands, generated docs, or documentation tooling.

## Security and responsible handling

- Follow the [authentication and secret-handling standard](docs/security/auth-and-secret-handling.md).
- Do not include real tenant IDs, access tokens, refresh tokens, device codes, client secrets, passwords, connection strings, exported customer data, or PII in issues, PRs, commits, logs, screenshots, or fixtures.
- Prefer placeholders and sanitized examples.
- If you accidentally expose sensitive data, revoke it immediately and notify the maintainers in the issue or PR without reposting the secret.

## Review culture

Reviews should be direct, kind, and specific. Explain architectural concerns with links to ADRs or standards. Contributors are encouraged to ask questions early; maintainers should help turn good ideas into small, mergeable changes.
