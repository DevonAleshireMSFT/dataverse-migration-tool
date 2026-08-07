using DataverseMigrationTool.Application.Contracts.Solutions;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Infrastructure.Solutions;

public sealed class SupportedSolutionPreflightPolicy
{
    private static readonly HashSet<SolutionComponentKind> ConditionalKinds =
    [
        SolutionComponentKind.ClassicWorkflow,
        SolutionComponentKind.CloudFlow,
        SolutionComponentKind.SecurityRole,
        SolutionComponentKind.FieldSecurityProfile,
        SolutionComponentKind.Plugin,
        SolutionComponentKind.CanvasApp,
        SolutionComponentKind.ConnectionReference,
        SolutionComponentKind.EnvironmentVariable,
        SolutionComponentKind.CustomConnector,
        SolutionComponentKind.ReportOrTemplate,
        SolutionComponentKind.SlaOrRoutingRule,
        SolutionComponentKind.MobileOfflineProfile,
        SolutionComponentKind.OrganizationSetting
    ];

    private static readonly HashSet<SolutionComponentKind> DeferredKinds =
    [
        SolutionComponentKind.PowerPlatformCodeApp,
        SolutionComponentKind.SystemUserTeamOrBusinessUnit,
        SolutionComponentKind.RecordData,
        SolutionComponentKind.RoleAssignment,
        SolutionComponentKind.Secret,
        SolutionComponentKind.DefaultSolution,
        SolutionComponentKind.InternalOrUndocumentedOperation
    ];

    public ValidationReport Evaluate(SolutionMigrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<ValidationFinding> findings = [];

        if (string.IsNullOrWhiteSpace(request.SourceSolutionUniqueName))
        {
            findings.Add(Blocker("DMT-SOLUTION-001", "A custom unmanaged source solution unique name is required.", "solution"));
        }
        else if (IsDefaultSolution(request.SourceSolutionUniqueName))
        {
            findings.Add(Blocker("DMT-SOLUTION-002", "Default-solution export/import is deferred; select a custom unmanaged source solution.", request.SourceSolutionUniqueName));
        }

        if (request.Source.Cloud != request.Target.Cloud)
        {
            findings.Add(Blocker(
                "DMT-SOLUTION-003",
                $"Cross-cloud solution migration from {request.Source.Cloud} to {request.Target.Cloud} is blocked pending an explicit security/compliance gate.",
                "target"));
        }

        if (request.ExportMode == SolutionExportMode.Unmanaged)
        {
            findings.Add(new ValidationFinding(
                "DMT-SOLUTION-004",
                "Unmanaged target imports are allowed only for intentional development/sandbox rehearsal; managed export/import is preferred downstream.",
                ValidationSeverity.Warning,
                "Solution ALM",
                request.SourceSolutionUniqueName));
        }

        foreach (SolutionComponentPreflightCheck component in request.ComponentPreflightChecks)
        {
            if (DeferredKinds.Contains(component.Kind))
            {
                findings.Add(Blocker(
                    "DMT-SOLUTION-010",
                    $"{component.Kind} '{component.Name}' has no supported MVP movement path in this tool and is deferred/excluded by the accepted spike.",
                    component.Name));
                continue;
            }

            if (component.ContainsSecretLikeValue)
            {
                findings.Add(Blocker(
                    "DMT-SOLUTION-011",
                    $"{component.Kind} '{component.Name}' appears to require or contain a secret value; solutions must not transport plaintext secrets.",
                    component.Name));
            }

            if (ConditionalKinds.Contains(component.Kind) && !component.TargetReady)
            {
                string diagnostic = string.IsNullOrWhiteSpace(component.Diagnostic)
                    ? "Target readiness preflight did not pass."
                    : component.Diagnostic!;
                findings.Add(Blocker(
                    "DMT-SOLUTION-012",
                    $"Conditional component {component.Kind} '{component.Name}' is blocked: {diagnostic}",
                    component.Name));
                continue;
            }

            if (ConditionalKinds.Contains(component.Kind) && component.TargetReady)
            {
                findings.Add(new ValidationFinding(
                    "DMT-SOLUTION-013",
                    $"Conditional component {component.Kind} '{component.Name}' passed target-readiness preflight.",
                    ValidationSeverity.Info,
                    "Solution ALM",
                    component.Name));
            }
        }

        return ValidationReport.FromFindings(findings);
    }

    private static bool IsDefaultSolution(string uniqueName) =>
        uniqueName.Equals("default", StringComparison.OrdinalIgnoreCase) ||
        uniqueName.Equals("DefaultSolution", StringComparison.OrdinalIgnoreCase) ||
        uniqueName.Equals("Active", StringComparison.OrdinalIgnoreCase);

    private static ValidationFinding Blocker(string ruleId, string message, string target) =>
        new(ruleId, message, ValidationSeverity.Blocker, "Solution ALM", target);
}
