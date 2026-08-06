# Dataverse Migration Tool Backend

This backend is the server-side migration engine described by ADR-001. The Power Platform Code App remains the admin UI/control plane; bulk Dataverse data and solution migration work runs here.

## Layers

- `DataverseMigrationTool.Domain` — core entities, value objects, and enums. It has no external package dependencies.
- `DataverseMigrationTool.Application` — use-case contracts and ports. It depends only on Domain.
- `DataverseMigrationTool.Infrastructure` — adapter implementations for Dataverse access, job storage, validation, and operation logging. It depends on Application and Domain.
- `DataverseMigrationTool.Api` — ASP.NET Core host and DI composition root. It depends on Application plus Infrastructure for registration.
- `*.Tests` — xUnit test projects for the Domain and Application layers.

## Dependency direction

Dependencies point inward:

`Api -> Application -> Domain`

`Api -> Infrastructure -> Application -> Domain`

Domain does not reference the Dataverse SDK, ASP.NET Core, UI frameworks, or infrastructure packages.

## Composition root

`DataverseMigrationTool.Api/Program.cs` is the composition root. It calls `AddMigrationConfiguration(builder.Configuration)` to register the validated configuration provider, then `AddInfrastructure()` to register the current provider, job store, validation engine, migration engine, and operation logger implementations.

## Configuration

Configuration contracts live in Application; Infrastructure only adapts ASP.NET Core `IConfiguration`. Source precedence is defaults, appsettings file values, environment variables, then optional composition/test overrides. Profiles configure distinct `Source` and `Target` Dataverse environments, including `DataverseCloud` selection for commercial, GCC, GCC High, and DoD endpoint resolution.

Secrets are represented only as `ClientSecretReference` values such as environment variable names or Key Vault secret names. Do not put plaintext client secrets in appsettings, tests, source code, or committed documentation.

## Security posture

Do not put secrets in source code or appsettings files. Dataverse connection details are placeholders for configuration, managed identity, federated credentials, or another approved Microsoft-supported authentication flow. Government and GCC-High endpoints must be selected through configuration rather than hard-coded public cloud assumptions.

## Commands

```powershell
dotnet build src\backend
dotnet test src\backend
dotnet run --project src\backend\DataverseMigrationTool.Api
```
