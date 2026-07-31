# Installation guide

Use this guide to prepare a workstation or build agent for dataverse-migration-tool.

> ℹ️ The repository does not yet publish an installable application package. Use this guide to standardize prerequisites and deployment expectations while the implementation is being built.

## Prerequisites

Install these tools on every operator or contributor machine:

- PowerShell 7 or later
- .NET SDK 9
- Node.js LTS
- Power Platform CLI (`pac`)
- Git
- Access to source and target Power Platform environments

## Access prerequisites

Before you install or run the tool, confirm that you have:

- A licensed account in each tenant you need to access
- Environment Maker or System Administrator rights appropriate to the migration scope
- Approval to move data and solution components between the selected environments
- A secure location for environment-specific configuration values

## Enterprise deployment considerations

For enterprise deployments:

- Install tooling on managed workstations or controlled build agents.
- Route all configuration through approved secret storage and environment variables.
- Use dedicated service principals or automation identities where policy allows.
- Restrict outbound network access to approved Microsoft endpoints only.
- Retain migration logs and validation reports according to your audit policy.

## Government deployment considerations

For GCC, GCC High, and DoD deployments:

- Confirm that every dependency is supported in the target cloud before rollout.
- Validate tenant-specific login endpoints and Dataverse environment URLs.
- Use identities approved for the enclave and avoid cross-cloud service connections.
- Store logs, exports, and backups only in approved government-compliant locations.
- Review conditional access, device compliance, and boundary-protection requirements before execution.

## Installation checklist

Use this checklist for each workstation or runner:

1. Install the prerequisite tools.
2. Sign in to the required Power Platform tenants with approved identities.
3. Confirm `pac` can enumerate the source and target environments.
4. Confirm .NET and Node versions match the project baseline.
5. Record the machine, identity, and environment scope in the migration change record.

## Related documentation

- [Configuration guide](configuration.md)
- [Migration runbook](runbooks/migration-runbook.md)
