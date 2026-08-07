# Project Context

- **Owner:** Devon Aleshire
- **Project:** Power Platform Code App to migrate Dataverse data and solution components between Power Platform environments — secure, repeatable, resumable, incremental & full migrations, validation reports, rollback guidance. Open-source, enterprise + government (GCC/High) ready.
- **Stack:** Power Platform Code Apps, PCF, Dataverse Web API, Power Platform CLI (`pac`), .NET 9, C#, TypeScript, React, Fluent UI, GitHub Actions, Power Platform Pipelines, Power Platform Build Tools.
- **Architecture:** Clean architecture — Presentation / Application / Domain / Infrastructure; Dataverse Provider, Migration Engine, Validation Engine, Logging Framework, Configuration Provider; all DI.
- **Principles:** API-first, Microsoft-supported APIs only, secure by default, extensible (plug-ins), observable, testable, maintainable, fully documented.
- **My role:** Migration & API Integration Engineer — migration engine, ordering, relationship preservation, incremental/full, checkpoints/resume, rollback, hot-path performance.
- **Created:** 2026-07-30

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-07-30T15:52:36-07:00): Repository is now live at DevonAleshireMSFT/dataverse-migration-tool (public).

📌 Team update (2026-07-31T11:32:32-07:00): Your backlog homes are Epics #3 and #4; Epic #9 is shared cross-team work.

📌 Team update (2026-08-06T22:53:26-07:00): Wave B delivered Issue #23 full data migration execution engine in PR #67.

📌 Team update (2026-08-06T23:25:11-07:00): Wave C delivered Issue #24 incremental/delta migration strategy spike in PR #68, documenting change-tracking-first deltas, successful-write checkpoint token persistence, alternate-key upsert, full re-scan fallback, and `modifiedon` best-effort limits.

📌 Team update (2026-08-06T23:25:11-07:00): Wave C delivered Issue #25 checkpoint/resume/idempotency feature in PR #69, adding durable migration checkpoints, idempotent resume behavior, capped retries, redacted failure guidance, run read-model checkpoint exposure, and POST `/api/migration-jobs/{jobId}/resume`.
