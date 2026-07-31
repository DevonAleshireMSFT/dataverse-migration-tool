# Contributing

Thank you for contributing to dataverse-migration-tool. This repository is still in early scaffolding, so every pull request should improve both the implementation and the operator experience.

## Before you start

- Review the [documentation hub](docs/README.md) for project context.
- Check open issues before starting work.
- Keep changes small and focused.
- Update documentation in the same pull request when behavior changes.

## Coding standards

Use these standards for all code and documentation changes:

- Prefer small, composable units over large, cross-cutting changes.
- Keep security and government-cloud compatibility as first-order requirements.
- Preserve resumability, validation, and rollback behavior in migration flows.
- Avoid hard-coded tenant identifiers, environment URLs, secrets, or credentials.
- Use descriptive names that match Dataverse and Power Platform terminology.
- Add or update tests when the repository has test coverage for the area you change.
- Write documentation in sentence case, active voice, and second person.

## Definition of Ready

An issue is ready for implementation when:

- The problem statement explains the user or operator outcome.
- Scope boundaries are clear.
- Acceptance criteria are testable or reviewable.
- Dependencies, permissions, and environment assumptions are identified.
- Security, compliance, and government-cloud considerations are called out when relevant.

## Definition of Done

Work is done when:

- The change satisfies the acceptance criteria.
- Documentation and runbooks reflect the final behavior.
- Existing tests for the affected area pass, or the pull request explains why no tests exist.
- The change does not introduce secrets, unsafe defaults, or avoidable compliance risks.
- Reviewers can understand deployment, validation, resume, and rollback impact from the pull request.

## Pull request checklist

Before you request review:

- Confirm the change is scoped to the issue.
- Re-read modified docs and UI text for clarity and consistency.
- Run the existing validation commands for the area you changed.
- Note any follow-up work that is intentionally out of scope.

## Related documentation

- [Documentation hub](docs/README.md)
- [Architecture reference](docs/reference/architecture.md)
- [Migration runbook](docs/runbooks/migration-runbook.md)
