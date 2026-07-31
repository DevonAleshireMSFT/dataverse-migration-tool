# Dataverse Migration Tool Code App

Presentation layer for the Dataverse Migration Tool. This Power Platform Code App is a React, TypeScript, Vite, and Fluent UI v9 app rooted at `src/app`.

## Prerequisites

- Node.js 22 LTS and npm 10+
- Power Platform CLI (`pac`) 2.6.4+ with `pac code`
- Active `pac` authentication for the target environment

## Local workflow

```powershell
npm install
npm run dev
npm run build
npm run lint
npm run typecheck
```

Use `npm run format` before broad formatting changes, or `npm run format:check` to verify formatting.

## Code App workflow

We currently standardize on `pac code` because the `power-apps` npm CLI is not part of the local toolchain yet.

```powershell
pac code init --displayName "Dataverse Migration Tool" --description "Presentation layer for Dataverse Migration Tool"
pac code push
```

Do not run `pac code push` until the app shell has been reviewed.

## Solution mapping

The app belongs with the `DataverseMigrationTool` Dataverse solution in `src/solutions/DataverseMigrationTool`, publisher prefix `dvmig`, version `0.4.0.0`. This package version is aligned at `0.4.0`; adding the Code App to the solution is a follow-up after registration and review.

Future work can reassess the `power-apps` npm CLI if Microsoft guidance or project tooling changes.
