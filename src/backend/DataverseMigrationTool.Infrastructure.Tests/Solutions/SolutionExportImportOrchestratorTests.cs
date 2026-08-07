using DataverseMigrationTool.Application.Contracts.Solutions;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Validation;
using DataverseMigrationTool.Infrastructure.Solutions;

namespace DataverseMigrationTool.Infrastructure.Tests.Solutions;

public sealed class SolutionExportImportOrchestratorTests
{
    [Theory]
    [InlineData(SolutionExportMode.Managed)]
    [InlineData(SolutionExportMode.Unmanaged)]
    public async Task StartAsync_surfaces_managed_or_unmanaged_export_mode(SolutionExportMode mode)
    {
        FakeSolutionAlmProvider provider = new()
        {
            TerminalStatuses = new Queue<SolutionImportJobStatus>(
            [
                new SolutionImportJobStatus(Guid.Empty, SolutionImportStatus.Succeeded, 100, Array.Empty<SolutionImportDiagnostic>(), DateTimeOffset.UtcNow)
            ])
        };
        SolutionMigrationRun run = await CreateOrchestrator(provider).StartAsync(Request(mode, importPollIntervalMilliseconds: 0));

        Assert.Equal(SolutionMigrationWorkflowStatus.Completed, run.Status);
        Assert.NotNull(run.Export);
        Assert.Equal(mode, run.Export.Mode);
        Assert.Equal(mode, provider.ExportModes.Single());
        if (mode == SolutionExportMode.Unmanaged)
        {
            Assert.Contains(run.ValidationReport.Warnings, finding => finding.RuleId == "DMT-SOLUTION-004");
        }
    }

    [Fact]
    public async Task StartAsync_stops_before_export_when_preflight_has_blocker()
    {
        FakeSolutionAlmProvider provider = new();
        SolutionMigrationRequest request = Request(
            SolutionExportMode.Managed,
            [new SolutionComponentPreflightCheck(SolutionComponentKind.CloudFlow, "Notify approver", TargetReady: false, "Target connection reference is not mapped.")],
            importPollIntervalMilliseconds: 0);

        SolutionMigrationRun run = await CreateOrchestrator(provider).StartAsync(request);

        Assert.Equal(SolutionMigrationWorkflowStatus.PreflightBlocked, run.Status);
        Assert.True(run.ValidationReport.Failed);
        Assert.Contains(run.ValidationReport.Blockers, finding => finding.RuleId == "DMT-SOLUTION-012");
        Assert.Equal(0, provider.ExportCallCount);
        Assert.Equal(0, provider.ImportCallCount);
    }

    [Fact]
    public async Task StartAsync_surfaces_import_job_status_and_diagnostics()
    {
        Guid importJobId = Guid.NewGuid();
        SolutionImportDiagnostic diagnostic = new("MissingDependency", "Install the required base solution.", ValidationSeverity.Blocker, "account");
        FakeSolutionAlmProvider provider = new()
        {
            ImportJobId = importJobId,
            TerminalStatuses = new Queue<SolutionImportJobStatus>(
            [
                new SolutionImportJobStatus(importJobId, SolutionImportStatus.Failed, 100, [diagnostic], DateTimeOffset.UtcNow)
            ])
        };

        SolutionMigrationRun run = await CreateOrchestrator(provider).StartAsync(Request(SolutionExportMode.Managed, importPollIntervalMilliseconds: 0));

        Assert.Equal(SolutionMigrationWorkflowStatus.Failed, run.Status);
        Assert.NotNull(run.ImportJob);
        Assert.Equal(importJobId, run.ImportJob.ImportJobId);
        Assert.Equal(SolutionImportStatus.Failed, run.ImportJob.Status);
        Assert.Equal(diagnostic, Assert.Single(run.ImportJob.Diagnostics));
    }

    [Fact]
    public async Task StartAsync_merges_solution_preflight_with_shared_validation_report()
    {
        FakeSolutionAlmProvider provider = new()
        {
            ProviderValidationReport = ValidationReport.FromFindings([
                new ValidationFinding("DMT-PROVIDER-001", "Provider readiness warning.", ValidationSeverity.Warning, "Solution ALM", "target")
            ]),
            TerminalStatuses = new Queue<SolutionImportJobStatus>(
            [
                new SolutionImportJobStatus(Guid.Empty, SolutionImportStatus.Succeeded, 100, Array.Empty<SolutionImportDiagnostic>(), DateTimeOffset.UtcNow)
            ])
        };
        SolutionMigrationRequest request = Request(
            SolutionExportMode.Managed,
            [new SolutionComponentPreflightCheck(SolutionComponentKind.ConnectionReference, "shared_commondataservice", TargetReady: true)],
            importPollIntervalMilliseconds: 0);

        SolutionMigrationRun run = await CreateOrchestrator(provider).StartAsync(request);

        Assert.True(run.ValidationReport.Passed);
        Assert.Contains(run.ValidationReport.Findings, finding => finding.RuleId == "DMT-SOLUTION-013");
        Assert.Contains(run.ValidationReport.Findings, finding => finding.RuleId == "DMT-PROVIDER-001");
        Assert.Equal(1, run.ValidationReport.Counts.Warnings);
        Assert.True(run.ValidationReport.Counts.Infos >= 1);
    }

    [Fact]
    public async Task StartAsync_blocks_deferred_components_without_unsupported_api_path()
    {
        FakeSolutionAlmProvider provider = new();
        SolutionMigrationRequest request = Request(
            SolutionExportMode.Managed,
            [new SolutionComponentPreflightCheck(SolutionComponentKind.PowerPlatformCodeApp, "Migration Tool Code App", TargetReady: true)],
            importPollIntervalMilliseconds: 0);

        SolutionMigrationRun run = await CreateOrchestrator(provider).StartAsync(request);

        Assert.Equal(SolutionMigrationWorkflowStatus.PreflightBlocked, run.Status);
        Assert.Contains(run.ValidationReport.Blockers, finding => finding.RuleId == "DMT-SOLUTION-010");
        Assert.Equal(0, provider.ExportCallCount);
    }

    private static SolutionExportImportOrchestrator CreateOrchestrator(FakeSolutionAlmProvider provider)
        => new(provider, new InMemorySolutionMigrationRunStore(), new CapturingOperationLogger(), new SupportedSolutionPreflightPolicy());

    private static SolutionMigrationRequest Request(
        SolutionExportMode mode,
        IReadOnlyCollection<SolutionComponentPreflightCheck>? checks = null,
        int importPollIntervalMilliseconds = 1000) =>
        new(
            CreateEnvironment("Source"),
            CreateEnvironment("Target"),
            "DataverseMigrationTool",
            mode,
            checks,
            ImportPollIntervalMilliseconds: importPollIntervalMilliseconds,
            MaxImportPollAttempts: 2);

    private static EnvironmentProfile CreateEnvironment(string name) => new(
        name,
        new Uri($"https://{name.ToLowerInvariant()}.crm.dynamics.com"),
        Guid.NewGuid(),
        DataverseCloud.Public);

    private sealed class FakeSolutionAlmProvider : ISolutionAlmProvider
    {
        public int ExportCallCount { get; private set; }
        public int ImportCallCount { get; private set; }
        public Guid ImportJobId { get; set; } = Guid.NewGuid();
        public List<SolutionExportMode> ExportModes { get; } = [];
        public Queue<SolutionImportJobStatus> TerminalStatuses { get; set; } = new();
        public ValidationReport ProviderValidationReport { get; set; } = ValidationReport.Empty;

        public Task<SolutionSelectionResult> EnsureUnmanagedSourceSolutionAsync(SolutionMigrationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SolutionSelectionResult(request.SourceSolutionUniqueName, IsUnmanaged: true, "1.0.0.0", "ok"));

        public Task<ValidationReport> ValidateDependenciesAndTargetReadinessAsync(SolutionMigrationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProviderValidationReport);

        public Task<SolutionExportResult> ExportSolutionAsync(SolutionMigrationRequest request, CancellationToken cancellationToken = default)
        {
            ExportCallCount++;
            ExportModes.Add(request.ExportMode);
            return Task.FromResult(new SolutionExportResult(request.SourceSolutionUniqueName, request.ExportMode, "solution.zip", DateTimeOffset.UtcNow));
        }

        public Task<SolutionImportStartResult> ImportSolutionAsync(SolutionMigrationRequest request, SolutionExportResult export, CancellationToken cancellationToken = default)
        {
            ImportCallCount++;
            return Task.FromResult(new SolutionImportStartResult(ImportJobId, ImportJobId.ToString("D"), DateTimeOffset.UtcNow));
        }

        public Task<SolutionImportJobStatus> GetImportJobStatusAsync(SolutionMigrationRequest request, Guid importJobId, CancellationToken cancellationToken = default)
        {
            if (TerminalStatuses.Count == 0)
            {
                return Task.FromResult(new SolutionImportJobStatus(importJobId, SolutionImportStatus.Succeeded, 100, Array.Empty<SolutionImportDiagnostic>(), DateTimeOffset.UtcNow));
            }

            SolutionImportJobStatus status = TerminalStatuses.Dequeue();
            return Task.FromResult(status with { ImportJobId = importJobId });
        }
    }

    private sealed class CapturingOperationLogger : IOperationLogger
    {
        public Task RecordAsync(Guid jobId, string operation, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
