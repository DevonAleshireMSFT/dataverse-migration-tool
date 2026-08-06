# Dataverse authentication and secret-handling standard

## Supported identity pattern

Dataverse token acquisition uses MSAL.NET through `IDataverseTokenProvider`. The provider receives the `EnvironmentProfile` and resolved `DataverseEndpoint` for every request, honors `endpoint.Scopes`, and builds a tenant-specific authority from the environment tenant ID plus the configured cloud authority host.

Required non-secret configuration:

- `DataverseAuth:ClientId`: Entra public-client application ID for delegated auth.
- `DataverseAuth:AuthorityHosts:Public`: commercial authority host.
- `DataverseAuth:AuthorityHosts:Gcc`: GCC authority host.
- `DataverseAuth:AuthorityHosts:GccHigh`: GCC High authority host.
- `DataverseAuth:AuthorityHosts:Dod`: DoD authority host.

Authority hosts are configuration, not secrets. Keep them environment-specific so sovereign tenants use the correct Microsoft Entra instance.

## Tenant and environment boundary

Tenant ID, cloud, Dataverse resource, and scopes must come from the current environment and endpoint. Do not store tenant, environment, or token values in global mutable state. Token cache partitions must include the client ID, tenant ID, cloud, authority host, and Dataverse resource.

## Secret handling

Do not put client secrets, certificates, refresh tokens, access tokens, device codes, passwords, or connection strings in source, logs, committed configuration, or `.squad` state. Prefer secretless flows: interactive auth, device code with a trusted prompt, managed identity, workload identity federation, or Key Vault references. If a future deployment needs a confidential credential, store only a secret name or Key Vault reference in configuration.

## Logging rules

Never log bearer tokens, refresh tokens, device codes, MSAL result payloads, authorization headers, or connection strings. Log only non-secret operational metadata such as environment name, tenant ID, cloud, and whether a trusted prompt is required.
