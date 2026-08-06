using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Infrastructure.Dataverse;

/// <summary>
/// Resolves Dataverse Web API and Microsoft Entra authority endpoints from the configured cloud.
/// Environment URLs remain configuration-driven so GCC, GCC High, and DoD tenants are not forced
/// through public-cloud Dataverse hosts.
/// </summary>
public sealed class DefaultDataverseEndpointResolver : IDataverseEndpointResolver
{
    private static readonly Uri PublicAuthorityHost = new("https://login.microsoftonline.com/");
    private static readonly Uri UsGovernmentAuthorityHost = new("https://login.microsoftonline.us/");

    public DataverseEndpoint Resolve(EnvironmentProfile environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        Uri environmentUrl = NormalizeEnvironmentUrl(environment.Url);
        Uri webApiBaseUrl = new(environmentUrl, "api/data/v9.2/");
        Uri authorityHost = environment.Cloud switch
        {
            DataverseCloud.Public or DataverseCloud.Gcc => PublicAuthorityHost,
            DataverseCloud.GccHigh or DataverseCloud.Dod => UsGovernmentAuthorityHost,
            _ => throw new ArgumentOutOfRangeException(nameof(environment), environment.Cloud, "Unsupported Dataverse cloud.")
        };

        string scope = $"{environmentUrl.GetLeftPart(UriPartial.Authority)}/.default";

        return new DataverseEndpoint(
            Cloud: environment.Cloud,
            EnvironmentUrl: environmentUrl,
            WebApiBaseUrl: webApiBaseUrl,
            AuthorityHost: authorityHost,
            Resource: new Uri(environmentUrl.GetLeftPart(UriPartial.Authority)),
            Scopes: [scope]);
    }

    private static Uri NormalizeEnvironmentUrl(Uri environmentUrl)
    {
        if (!environmentUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("Dataverse environment URL must be absolute.", nameof(environmentUrl));
        }

        if (environmentUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Dataverse environment URL must use HTTPS.", nameof(environmentUrl));
        }

        return new Uri(environmentUrl.GetLeftPart(UriPartial.Authority));
    }
}
