# API reference

Use this page as the contract map for the external APIs and planned internal seams used by dataverse-migration-tool.

## External dependencies

The tool is expected to integrate with these external surfaces:

| Surface | Purpose |
| --- | --- |
| Dataverse Web API | Read and write table data, metadata, and relationships |
| Power Platform CLI (`pac`) | Environment discovery, solution operations, and automation support |
| Microsoft Entra ID | Authentication and token acquisition |

## Planned internal contracts

Document and keep these seams stable as the implementation lands:

| Contract | Responsibility |
| --- | --- |
| Environment provider | Resolves source and target environment details |
| Authentication provider | Acquires tokens or delegated credentials safely |
| Migration planner | Determines ordering, batching, and dependency handling |
| Checkpoint store | Persists and reloads resume state |
| Validation reporter | Produces operator-readable results and evidence |

## Contract expectations

Each contract should:

- Fail with actionable errors
- Avoid leaking secrets or raw tokens
- Support resumable workflows where applicable
- Carry enough context for enterprise and government audit trails

## Versioning guidance

As the codebase grows:

- Version breaking contract changes explicitly.
- Document new required permissions alongside new API calls.
- Add examples only when they match the implemented behavior.

## Related documentation

- [Architecture reference](architecture.md)
- [Contribution guide](../../CONTRIBUTING.md)
