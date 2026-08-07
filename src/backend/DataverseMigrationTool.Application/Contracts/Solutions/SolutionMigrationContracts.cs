using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Application.Contracts.Solutions;

public sealed record SolutionMigrationRequest(
    EnvironmentProfile Source,
    EnvironmentProfile Target,
    string SourceSolutionUniqueName,
    SolutionExportMode ExportMode = SolutionExportMode.Managed,
    IReadOnlyCollection<SolutionComponentPreflightCheck>? ComponentPreflightChecks = null,
    bool IncludeRequiredComponents = true,
    bool PublishWorkflows = false,
    int ImportPollIntervalMilliseconds = 1000,
    int MaxImportPollAttempts = 120)
{
    public IReadOnlyCollection<SolutionComponentPreflightCheck> ComponentPreflightChecks { get; init; } =
        ComponentPreflightChecks?.ToArray() ?? Array.Empty<SolutionComponentPreflightCheck>();
}

public enum SolutionExportMode
{
    Managed,
    Unmanaged
}

public enum SolutionComponentKind
{
    Table,
    Column,
    Relationship,
    Choice,
    AlternateKey,
    Form,
    View,
    Chart,
    Dashboard,
    BusinessRule,
    ClassicWorkflow,
    CloudFlow,
    SecurityRole,
    FieldSecurityProfile,
    Plugin,
    WebResource,
    ModelDrivenApp,
    CanvasApp,
    PowerPlatformCodeApp,
    ConnectionReference,
    EnvironmentVariable,
    CustomConnector,
    ReportOrTemplate,
    SlaOrRoutingRule,
    MobileOfflineProfile,
    OrganizationSetting,
    SystemUserTeamOrBusinessUnit,
    RecordData,
    RoleAssignment,
    Secret,
    DefaultSolution,
    InternalOrUndocumentedOperation
}

public sealed record SolutionComponentPreflightCheck(
    SolutionComponentKind Kind,
    string Name,
    bool TargetReady,
    string? Diagnostic = null,
    bool ContainsSecretLikeValue = false);

public sealed record SolutionMigrationRun(
    Guid Id,
    SolutionMigrationRequest Request,
    SolutionMigrationWorkflowStatus Status,
    ValidationReport ValidationReport,
    SolutionExportResult? Export = null,
    SolutionImportJobStatus? ImportJob = null,
    DateTimeOffset StartedAt = default,
    DateTimeOffset? CompletedAt = null,
    string? FailureMessage = null)
{
    public bool Succeeded => Status == SolutionMigrationWorkflowStatus.Completed;
}

public enum SolutionMigrationWorkflowStatus
{
    Pending,
    Validating,
    PreflightBlocked,
    Exporting,
    Importing,
    Completed,
    Failed
}

public sealed record SolutionSelectionResult(
    string SolutionUniqueName,
    bool IsUnmanaged,
    string? Version,
    string Message);

public sealed record SolutionExportResult(
    string SolutionUniqueName,
    SolutionExportMode Mode,
    string ArtifactName,
    DateTimeOffset ExportedAt,
    string? Version = null);

public sealed record SolutionImportStartResult(
    Guid ImportJobId,
    string CorrelationId,
    DateTimeOffset StartedAt);

public sealed record SolutionImportJobStatus(
    Guid ImportJobId,
    SolutionImportStatus Status,
    int PercentComplete,
    IReadOnlyCollection<SolutionImportDiagnostic> Diagnostics,
    DateTimeOffset CheckedAt)
{
    public bool IsTerminal => Status is SolutionImportStatus.Succeeded or SolutionImportStatus.Failed;
}

public enum SolutionImportStatus
{
    NotStarted,
    Running,
    Succeeded,
    Failed
}

public sealed record SolutionImportDiagnostic(
    string Code,
    string Message,
    ValidationSeverity Severity,
    string? Component = null);
