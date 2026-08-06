using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace DataverseMigrationTool.Infrastructure.Dataverse.Auth;

/// <summary>
/// Resolves the Microsoft Entra authority host and tenant authority used for Dataverse token acquisition.
/// Authority hosts can be overridden per cloud through configuration so sovereign tenants are not forced
/// through the public-cloud authority instance.
/// </summary>
public sealed class DataverseAuthorityResolver(IConfiguration configuration)
{
    /// <summary>
    /// Configuration section that contains cloud-specific authority host overrides.
    /// Supported keys are <c>Public</c>, <c>Gcc</c>, <c>GccHigh</c>, and <c>Dod</c>.
    /// </summary>
    public const string AuthorityHostsSectionName = "DataverseAuth:AuthorityHosts";

    /// <summary>
    /// Resolves the configured authority host for the environment cloud, falling back to the resolved
    /// endpoint authority when no override is configured.
    /// </summary>
    /// <param name="environment">The Dataverse environment that owns the tenant and cloud boundary.</param>
    /// <param name="endpoint">The endpoint resolved for the same environment.</param>
    /// <returns>The HTTPS authority host for the environment cloud.</returns>
    /// <exception cref="ArgumentException">Thrown when the environment and endpoint clouds differ or configuration is invalid.</exception>
    public Uri ResolveAuthorityHost(EnvironmentProfile environment, DataverseEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(endpoint);

        if (environment.Cloud != endpoint.Cloud)
        {
            throw new ArgumentException(
                $"Environment cloud '{environment.Cloud}' does not match endpoint cloud '{endpoint.Cloud}'.",
                nameof(endpoint));
        }

        string? configuredAuthorityHost = configuration[$"{AuthorityHostsSectionName}:{environment.Cloud}"];
        Uri authorityHost = string.IsNullOrWhiteSpace(configuredAuthorityHost)
            ? endpoint.AuthorityHost
            : new Uri(configuredAuthorityHost, UriKind.Absolute);

        ValidateAuthorityHost(authorityHost);
        return EnsureTrailingSlash(authorityHost);
    }

    /// <summary>
    /// Builds the tenant-specific authority URI used by MSAL for token acquisition.
    /// </summary>
    /// <param name="authorityHost">The resolved Microsoft Entra authority host.</param>
    /// <param name="tenantId">The explicit tenant ID associated with the Dataverse environment.</param>
    /// <returns>The tenant authority URI.</returns>
    public static Uri CreateTenantAuthority(Uri authorityHost, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(authorityHost);

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID must be explicit for Dataverse token acquisition.", nameof(tenantId));
        }

        ValidateAuthorityHost(authorityHost);
        return new Uri(EnsureTrailingSlash(authorityHost), tenantId.ToString("D"));
    }

    private static void ValidateAuthorityHost(Uri authorityHost)
    {
        if (!authorityHost.IsAbsoluteUri || authorityHost.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Microsoft Entra authority host must be an absolute HTTPS URI.", nameof(authorityHost));
        }
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        string value = uri.AbsoluteUri;
        return value.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(value + "/");
    }
}


