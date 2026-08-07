# Project Context

- **Owner:** Devon Aleshire
- **Project:** Power Platform Code App to migrate Dataverse data and solution components between Power Platform environments — secure, repeatable, resumable, incremental & full migrations, validation reports, rollback guidance. Open-source, enterprise + government (GCC/High) ready.
- **Stack:** Power Platform Code Apps, PCF, Dataverse Web API, Power Platform CLI (`pac`), .NET 9, C#, TypeScript, React, Fluent UI, GitHub Actions, Power Platform Pipelines, Power Platform Build Tools.
- **Architecture:** Clean architecture — Presentation / Application / Domain / Infrastructure; Dataverse Provider, Migration Engine, Validation Engine, Logging Framework, Configuration Provider; all DI.
- **Principles:** API-first, Microsoft-supported APIs only, secure by default, extensible (plug-ins), observable, testable, maintainable, fully documented.
- **My role:** Power Platform & Dataverse Engineer — Dataverse Web API, metadata discovery, environment comparison, solution components, `pac` CLI, throttling/batching.
- **Created:** 2026-07-30

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-07-30T15:52:36-07:00): Repository is now live at DevonAleshireMSFT/dataverse-migration-tool (public).

📌 Team update (2026-07-31T11:32:32-07:00): Your backlog home is Epic #2; Epic #9 is shared cross-team work.

📌 Team update (2026-07-31T12:40:00-07:00): `pac code init` / `pac code push` are currently blocked for GFIM-DEV in GCC High/UsGovHigh because pac 2.6.4 environment lookup is not sovereign-cloud-aware; track/confirm the fix before registration or solution inclusion — decided by Squad (Coordinator).

📌 Team update (2026-08-05T22:54:08-07:00): Wave 1 delivered Issue #18 Dataverse provider contracts and auth-handoff seam in PR #54.

📌 Team update (2026-08-06T22:24:36-07:00): Wave A delivered Issue #20 metadata discovery read models, discovery service contracts, and TTL/invalidation caching in PR #64.

📌 Team update (2026-08-06T22:53:26-07:00): Wave B delivered Issue #21 environment compare and migration readiness report in PR #66.

📌 Team update (2026-08-06T23:25:11-07:00): Wave C delivered Issue #27 supported solution-component migration surface spike in PR #70, documenting the MVP supported-ALM path, readiness/dependency gates, ImportJob diagnostics, pac/Build Tools wrappers, Solution API orchestration, and deferred unsupported/destructive surfaces.
