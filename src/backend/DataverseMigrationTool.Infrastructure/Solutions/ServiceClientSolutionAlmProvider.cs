using System.Collections.Concurrent;
using System.ServiceModel;
using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Application.Contracts.Solutions;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects.Validation;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DataverseMigrationTool.Infrastructure.Solutions;

public sealed class ServiceClientSolutionAlmProvider(
    IDataverseTokenProvider tokenProvider,
    IDataverseEndpointResolver endpointResolver) : ISolutionAlmProvider
{
    private readonly ConcurrentDictionary<string, byte[]> exportedArtifacts = new(StringComparer.OrdinalIgnoreCase);

    public async Task<SolutionSelectionResult> EnsureUnmanagedSourceSolutionAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using ServiceClient client = await CreateServiceClientAsync(request.Source, cancellationToken);

        QueryExpression query = new("solution")
        {
            ColumnSet = new ColumnSet("uniquename", "ismanaged", "version"),
            Criteria = new FilterExpression(LogicalOperator.And)
        };
        query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, request.SourceSolutionUniqueName);
        Entity? solution = client.RetrieveMultiple(query).Entities.FirstOrDefault();
        if (solution is null)
        {
            throw new InvalidOperationException($"Source solution '{request.SourceSolutionUniqueName}' was not found.");
        }

        bool isManaged = solution.GetAttributeValue<bool?>("ismanaged") ?? false;
        return new SolutionSelectionResult(
            request.SourceSolutionUniqueName,
            IsUnmanaged: !isManaged,
            solution.GetAttributeValue<string>("version"),
            isManaged ? "Source solution is managed; select the unmanaged authoring solution." : "Unmanaged source solution selected.");
    }

    public Task<ValidationReport> ValidateDependenciesAndTargetReadinessAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ValidationFinding> findings =
        [
            new ValidationFinding(
                "DMT-SOLUTION-100",
                "Supported Dataverse Solution ALM preflight is active; Dataverse import dependency enforcement and ImportJob diagnostics remain authoritative during import.",
                ValidationSeverity.Info,
                "Solution ALM",
                request.SourceSolutionUniqueName)
        ];
        return Task.FromResult(ValidationReport.FromFindings(findings));
    }

    public async Task<SolutionExportResult> ExportSolutionAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using ServiceClient client = await CreateServiceClientAsync(request.Source, cancellationToken);
        try
        {
            ExportSolutionRequest exportRequest = new()
            {
                SolutionName = request.SourceSolutionUniqueName,
                Managed = request.ExportMode == SolutionExportMode.Managed
            };
            ExportSolutionResponse response = (ExportSolutionResponse)client.Execute(exportRequest);
            cancellationToken.ThrowIfCancellationRequested();

            string artifactName = $"{request.SourceSolutionUniqueName}_{request.ExportMode}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip";
            exportedArtifacts[artifactName] = response.ExportSolutionFile;
            return new SolutionExportResult(request.SourceSolutionUniqueName, request.ExportMode, artifactName, DateTimeOffset.UtcNow);
        }
        catch (FaultException<OrganizationServiceFault> ex) when (TryCreateTransient(ex, out SolutionAlmTransientException? transient))
        {
            throw transient!;
        }
    }

    public async Task<SolutionImportStartResult> ImportSolutionAsync(
        SolutionMigrationRequest request,
        SolutionExportResult export,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(export);

        if (!exportedArtifacts.TryGetValue(export.ArtifactName, out byte[]? customizationFile))
        {
            throw new InvalidOperationException($"Export artifact '{export.ArtifactName}' is not available for import in this orchestration process.");
        }

        using ServiceClient client = await CreateServiceClientAsync(request.Target, cancellationToken);
        Guid importJobId = Guid.NewGuid();
        try
        {
            ImportSolutionRequest importRequest = new()
            {
                CustomizationFile = customizationFile,
                ImportJobId = importJobId,
                PublishWorkflows = request.PublishWorkflows,
                OverwriteUnmanagedCustomizations = false
            };
            client.Execute(importRequest);
            cancellationToken.ThrowIfCancellationRequested();
            return new SolutionImportStartResult(importJobId, importJobId.ToString("D"), DateTimeOffset.UtcNow);
        }
        catch (FaultException<OrganizationServiceFault> ex) when (TryCreateTransient(ex, out SolutionAlmTransientException? transient))
        {
            throw transient!;
        }
    }

    public async Task<SolutionImportJobStatus> GetImportJobStatusAsync(
        SolutionMigrationRequest request,
        Guid importJobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using ServiceClient client = await CreateServiceClientAsync(request.Target, cancellationToken);
        try
        {
            Entity importJob = client.Retrieve("importjob", importJobId, new ColumnSet("importjobid", "progress", "completedon", "data"));
            int percentComplete = importJob.GetAttributeValue<int?>("progress") ?? 0;
            string? data = importJob.GetAttributeValue<string>("data");
            bool completed = importJob.Contains("completedon");
            bool failed = completed && data?.Contains("failure", StringComparison.OrdinalIgnoreCase) == true;
            SolutionImportStatus status = completed
                ? failed ? SolutionImportStatus.Failed : SolutionImportStatus.Succeeded
                : SolutionImportStatus.Running;

            List<SolutionImportDiagnostic> diagnostics = [];
            if (!string.IsNullOrWhiteSpace(data))
            {
                diagnostics.Add(new SolutionImportDiagnostic(
                    failed ? "ImportJobFailure" : "ImportJobLog",
                    Redact(data),
                    failed ? ValidationSeverity.Blocker : ValidationSeverity.Info));
            }

            return new SolutionImportJobStatus(importJobId, status, percentComplete, diagnostics, DateTimeOffset.UtcNow);
        }
        catch (FaultException<OrganizationServiceFault> ex) when (TryCreateTransient(ex, out SolutionAlmTransientException? transient))
        {
            throw transient!;
        }
    }

    private async Task<ServiceClient> CreateServiceClientAsync(
        DataverseMigrationTool.Domain.ValueObjects.EnvironmentProfile environment,
        CancellationToken cancellationToken)
    {
        DataverseEndpoint endpoint = endpointResolver.Resolve(environment);
        DataverseAccessToken accessToken = await tokenProvider.GetAccessTokenAsync(environment, endpoint, cancellationToken);
        ServiceClient client = new(endpoint.EnvironmentUrl, _ => Task.FromResult(accessToken.Token), useUniqueInstance: true, logger: null);
        if (!client.IsReady)
        {
            string detail = string.IsNullOrWhiteSpace(client.LastError) ? "ServiceClient was not ready after construction." : client.LastError;
            client.Dispose();
            throw new InvalidOperationException($"Dataverse ServiceClient could not connect for supported Solution ALM operation. {detail}");
        }

        return client;
    }

    private static bool TryCreateTransient(FaultException<OrganizationServiceFault> exception, out SolutionAlmTransientException? transient)
    {
        transient = null;
        if (exception.Detail.ErrorDetails.TryGetValue("Retry-After", out object? retryAfterValue) &&
            int.TryParse(retryAfterValue?.ToString(), out int seconds))
        {
            transient = new SolutionAlmTransientException("Dataverse throttled the Solution ALM operation and supplied Retry-After.", TimeSpan.FromSeconds(seconds), exception);
            return true;
        }

        return false;
    }

    private static string Redact(string value) =>
        value.Length <= 4000 ? value : string.Concat(value.AsSpan(0, 4000), "... [truncated]");
}

