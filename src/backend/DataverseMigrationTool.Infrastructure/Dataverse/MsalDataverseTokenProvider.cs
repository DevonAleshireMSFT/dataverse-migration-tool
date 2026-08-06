using System.Collections.Concurrent;
using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Infrastructure.Dataverse.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace DataverseMigrationTool.Infrastructure.Dataverse;

/// <summary>
/// MSAL-backed Dataverse token provider that keeps tenant, environment, authority, and scope boundaries explicit.
/// Tokens are returned only to the Dataverse handoff seam and are never logged or persisted by this provider.
/// </summary>
public sealed class MsalDataverseTokenProvider : IDataverseTokenProvider
{
    /// <summary>
    /// Configuration key containing the public-client application ID used for Dataverse delegated authentication.
    /// The value is an application identifier, not a secret.
    /// </summary>
    public const string ClientIdConfigurationKey = "DataverseAuth:ClientId";

    private readonly IConfiguration configuration;
    private readonly DataverseAuthorityResolver authorityResolver;
    private readonly IDataverseDeviceCodePrompt deviceCodePrompt;
    private readonly ILogger<MsalDataverseTokenProvider> logger;
    private readonly ConcurrentDictionary<TokenCachePartitionKey, IPublicClientApplication> applications = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MsalDataverseTokenProvider"/> class.
    /// </summary>
    /// <param name="configuration">Configuration used for non-secret auth settings such as client ID and authority hosts.</param>
    /// <param name="authorityResolver">Resolver for commercial and sovereign Microsoft Entra authority hosts.</param>
    /// <param name="deviceCodePrompt">Trusted prompt used when MSAL requires device-code interaction.</param>
    /// <param name="logger">Logger used for non-secret operational events.</param>
    public MsalDataverseTokenProvider(
        IConfiguration configuration,
        DataverseAuthorityResolver authorityResolver,
        IDataverseDeviceCodePrompt deviceCodePrompt,
        ILogger<MsalDataverseTokenProvider> logger)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.authorityResolver = authorityResolver ?? throw new ArgumentNullException(nameof(authorityResolver));
        this.deviceCodePrompt = deviceCodePrompt ?? throw new ArgumentNullException(nameof(deviceCodePrompt));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<DataverseAccessToken> GetAccessTokenAsync(
        EnvironmentProfile environment,
        DataverseEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(endpoint);
        cancellationToken.ThrowIfCancellationRequested();

        if (environment.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Environment tenant ID must be explicit for Dataverse token acquisition.", nameof(environment));
        }

        string[] scopes = endpoint.Scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (scopes.Length == 0)
        {
            throw new ArgumentException("Dataverse endpoint must provide at least one MSAL scope.", nameof(endpoint));
        }

        string clientId = GetConfiguredClientId();
        Uri authorityHost = authorityResolver.ResolveAuthorityHost(environment, endpoint);
        Uri tenantAuthority = DataverseAuthorityResolver.CreateTenantAuthority(authorityHost, environment.TenantId);
        TokenCachePartitionKey cacheKey = new(
            clientId,
            environment.TenantId,
            environment.Cloud,
            authorityHost.AbsoluteUri,
            endpoint.Resource.GetLeftPart(UriPartial.Authority));

        IPublicClientApplication application = applications.GetOrAdd(
            cacheKey,
            _ => CreatePublicClientApplication(clientId, tenantAuthority));

        AuthenticationResult result = await AcquireTokenAsync(application, scopes, environment, cancellationToken)
            .ConfigureAwait(false);

        return new DataverseAccessToken(result.AccessToken, result.ExpiresOn);
    }

    private string GetConfiguredClientId()
    {
        string? clientId = configuration[ClientIdConfigurationKey];
        if (string.IsNullOrWhiteSpace(clientId) || !Guid.TryParse(clientId, out _))
        {
            throw new InvalidOperationException(
                $"Dataverse authentication requires non-secret configuration '{ClientIdConfigurationKey}' with the Entra public-client application ID.");
        }

        return clientId;
    }

    private static IPublicClientApplication CreatePublicClientApplication(string clientId, Uri tenantAuthority) =>
        PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(tenantAuthority.AbsoluteUri)
            .WithRedirectUri("http://localhost")
            .Build();

    private async Task<AuthenticationResult> AcquireTokenAsync(
        IPublicClientApplication application,
        string[] scopes,
        EnvironmentProfile environment,
        CancellationToken cancellationToken)
    {
        IAccount? account = (await application.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();

        try
        {
            return await application
                .AcquireTokenSilent(scopes, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            logger.LogInformation(
                "Dataverse MSAL cache miss for environment {EnvironmentName} in tenant {TenantId}; requesting trusted device-code prompt without logging codes or tokens.",
                environment.Name,
                environment.TenantId);

            return await application
                .AcquireTokenWithDeviceCode(scopes, deviceCode => PresentDeviceCodeAsync(deviceCode, environment, cancellationToken))
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task PresentDeviceCodeAsync(
        DeviceCodeResult deviceCode,
        EnvironmentProfile environment,
        CancellationToken cancellationToken)
    {
        Uri verificationUri = new(deviceCode.VerificationUrl, UriKind.Absolute);
        DataverseDeviceCodePromptContext context = new(
            verificationUri,
            deviceCode.UserCode,
            deviceCode.ExpiresOn,
            environment.TenantId,
            environment.Name);

        await deviceCodePrompt.ShowAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private sealed record TokenCachePartitionKey(
        string ClientId,
        Guid TenantId,
        DataverseCloud Cloud,
        string AuthorityHost,
        string ResourceAuthority);
}

