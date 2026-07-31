# Squad Team

> dataverse-migration-tool

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Holden | Lead / Solution Architect | .squad/agents/holden/charter.md | 🏗️ Active |
| Naomi | Power Platform & Dataverse Engineer | .squad/agents/naomi/charter.md | 🔧 Active |
| Amos | Migration & API Integration Engineer | .squad/agents/amos/charter.md | ⚙️ Active |
| Alex | UI/UX · React · PCF Engineer | .squad/agents/alex/charter.md | ⚛️ Active |
| Bobbie | Security · Auth · Government Compliance | .squad/agents/bobbie/charter.md | 🔒 Active |
| Drummer | DevOps · CI/CD · Release Manager | .squad/agents/drummer/charter.md | ⚙️ Active |
| Prax | Test Automation Engineer | .squad/agents/prax/charter.md | 🧪 Active |
| Monica | Documentation & Technical Writer | .squad/agents/monica/charter.md | 📝 Active |
| Scribe | Session Logger / Memory | .squad/agents/scribe/charter.md | 📋 Built-in |
| Ralph | Work Monitor | .squad/agents/ralph/charter.md | 🔄 Built-in |
| Rai | RAI Reviewer | .squad/agents/Rai/charter.md | 🛡️ Built-in |
| Fact Checker | Verification & Devil's Advocate | .squad/agents/fact-checker/charter.md | 🔍 Built-in |


## Coding Agent

<!-- copilot-auto-assign: true -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion

## Project Context

- **Project:** dataverse-migration-tool
- **Owner:** Devon Aleshire
- **Description:** Power Platform Code App to migrate Dataverse data and solution components between Power Platform environments — secure, resumable, incremental & full migrations, validation reports, rollback guidance. Open-source, enterprise + government (GCC/High) ready.
- **Stack:** Power Platform Code Apps, PCF, Dataverse Web API, `pac` CLI, .NET 9, C#, TypeScript, React, Fluent UI, GitHub Actions, PP Pipelines, Build Tools.
- **Casting universe:** The Expanse
- **Created:** 2026-07-30
