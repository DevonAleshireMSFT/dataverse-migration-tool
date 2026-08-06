using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Infrastructure.Dataverse.Auth;
using Microsoft.Extensions.Configuration;

namespace DataverseMigrationTool.Infrastructure.Tests;

public sealed class DataverseAuthorityResolverTests
{
    [Fact]
    public void ResolveAuthorityHost_UsesConfiguredCloudAuthorityAndExplicitTenantBoundary()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataverseAuth:AuthorityHosts:Public"] = "https://login.microsoftonline.com/",
                ["DataverseAuth:AuthorityHosts:GccHigh"] = "https://login.microsoftonline.us/"
            })
            .Build();
        DataverseAuthorityResolver resolver = new(configuration);
        EnvironmentProfile publicEnvironment = CreateEnvironment(DataverseCloud.Public, "https://contoso.crm.dynamics.com");
        EnvironmentProfile gccHighEnvironment = CreateEnvironment(DataverseCloud.GccHigh, "https://contoso.crm9.dynamics.com");

        Uri publicAuthority = resolver.ResolveAuthorityHost(publicEnvironment, CreateEndpoint(publicEnvironment));
        Uri gccHighAuthority = resolver.ResolveAuthorityHost(gccHighEnvironment, CreateEndpoint(gccHighEnvironment));
        Uri tenantAuthority = DataverseAuthorityResolver.CreateTenantAuthority(gccHighAuthority, gccHighEnvironment.TenantId);

        Assert.Equal(new Uri("https://login.microsoftonline.com/"), publicAuthority);
        Assert.Equal(new Uri("https://login.microsoftonline.us/"), gccHighAuthority);
        Assert.Equal(new Uri($"https://login.microsoftonline.us/{gccHighEnvironment.TenantId:D}"), tenantAuthority);
    }

    [Fact]
    public void DeviceCodePromptContext_ToString_RedactsAuthenticationArtifacts()
    {
        DataverseDeviceCodePromptContext context = new(
            new Uri("https://microsoft.com/devicelogin"),
            "ABCD-EFGH",
            DateTimeOffset.Parse("2026-08-06T13:09:08-07:00"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "GFIM-DEV");

        string rendered = context.ToString();

        Assert.DoesNotContain(context.UserCode, rendered, StringComparison.Ordinal);
        Assert.Contains("Code redacted", rendered, StringComparison.Ordinal);
    }

    private static EnvironmentProfile CreateEnvironment(DataverseCloud cloud, string url) => new(
        "Environment",
        new Uri(url),
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        cloud);

    private static DataverseEndpoint CreateEndpoint(EnvironmentProfile environment) => new(
        environment.Cloud,
        environment.Url,
        new Uri(environment.Url, "api/data/v9.2/"),
        new Uri("https://configured-by-endpoint.example/"),
        new Uri(environment.Url.GetLeftPart(UriPartial.Authority)),
        [$"{environment.Url.GetLeftPart(UriPartial.Authority)}/.default"]);
}
