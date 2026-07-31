# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, ADRs, scope, code review | Holden | Layer boundaries, DI, repo structure, contracts, DoR/DoD, coding standards |
| Dataverse API, metadata, solution components | Naomi | Web API client, metadata discovery, environment compare, `pac solution`, throttling |
| Migration engine, ordering, resume, rollback | Amos | Execution planner, dependency graph, incremental/full, checkpoints, performance |
| UI, UX, React, Fluent UI, PCF | Alex | Code App screens, compare/scope views, migration monitor, validation/rollback UI |
| Security, auth, secrets, compliance | Bobbie | MSAL/Entra, threat model, secure-by-default, GCC/High readiness, security review |
| CI/CD, build/release, versioning | Drummer | GitHub Actions, PP Pipelines/Build Tools, packaging, release process, promotion |
| Testing, validation coverage, quality gates | Prax | Unit/integration/E2E, Validation Engine tests, test data, coverage thresholds |
| Docs, technical writing, runbooks | Monica | README, guides, reference docs, runbooks, contribution guide, docs-as-code |
| Code review | Holden (primary), Prax (test/quality) | Review PRs, check quality, enforce standards |
| Testing | Prax | Write tests, find edge cases, verify fixes |
| Scope & priorities | Holden + Devon (owner) | What to build next, trade-offs, decisions |
| Security review (hard gate) | Bobbie | Anything touching credentials, PII, access control |
| Session logging | Scribe | Automatic — never needs routing |
| RAI review | Rai | Content safety, bias checks, credential detection, ethical review |
| Verification / Devil's Advocate | Fact Checker | Claim verification, pre-mortem, challenge assumptions |
| Bug fixes (isolated, test-covered) | @copilot 🤖 | Single-file fixes, small scoped bugs with clear repro |
| Test coverage gaps | @copilot 🤖 | Adding missing unit tests for existing code |
| Documentation updates | @copilot 🤖 | README, inline comments, reference doc fixes |
| Lint / format / dependency bumps | @copilot 🤖 | Style cleanup, version bumps, boilerplate/scaffolding |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
