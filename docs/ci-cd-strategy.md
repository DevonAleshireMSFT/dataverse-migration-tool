# CI/CD baseline strategy

## Branch strategy

- `main` is the releasable branch. Every pull request into `main` must pass CI before merge.
- Feature work should land through short-lived branches, including Squad branches named `squad/{issue-number}-{kebab-case-slug}`.
- Pushes to `main` run the same baseline gates so the default branch stays green after merges.
- Releases are cut from `main` after the required gates pass.

## Baseline CI gates

The baseline GitHub Actions workflow runs on pull requests and pushes to `main` with least-privilege `contents: read` permissions and no deployment secrets.

### .NET backend

- Uses .NET SDK `9.0.x` for the `net9.0` backend projects.
- Restores `src/backend`.
- Builds `src/backend` in `Release` configuration.
- Tests `src/backend` in `Release` configuration.
- Publishes TRX test results as a workflow artifact for failed-run diagnostics.

### Code App

- Uses Node.js 22.
- Installs from `src/app/package-lock.json` with `npm ci` and npm dependency caching.
- Runs `npm run lint`.
- Runs `npm run build`.

### Coverage gate

TODO: Wire the coverage threshold and reporting gate defined by Prax in issue #30 once that work merges. The intended gate should run in CI before a pull request can merge to `main`.

### Dependency, formatting, and security gates

The first scaffold keeps CI focused on build, test, and lint without requiring secrets. Future hardening should add:

- Prettier format check for the Code App.
- NuGet and npm vulnerability audits with a documented severity threshold.
- Dependabot or equivalent dependency update policy.
- CodeQL or another static analysis gate once the team agrees on alert handling.

## Versioning

Use semantic versioning: `MAJOR.MINOR.PATCH`.

- Increment `MAJOR` for incompatible public changes.
- Increment `MINOR` for backwards-compatible features.
- Increment `PATCH` for backwards-compatible fixes.
- Pre-1.0 releases may still change quickly, but version changes should communicate operator impact clearly.
- Release tags should use `vMAJOR.MINOR.PATCH` once release automation is added.

## Deferred deployment path

This baseline intentionally does not push or deploy to Power Platform. The project remains local-first while sovereign Code App tooling is validated.

Future deployment phases should evaluate:

1. Power Platform Pipelines for environment promotion.
2. Power Platform Build Tools or `pac` CLI in GitHub Actions for solution import/export once supported for the target cloud.
3. GitHub environment protection rules for test and production promotions.
4. Secret storage only through GitHub Actions secrets/environments or federated identity; never plaintext workflow credentials.

Deployment automation should be added only after the team confirms supported tooling for the target Power Platform cloud and the required environment strategy.
