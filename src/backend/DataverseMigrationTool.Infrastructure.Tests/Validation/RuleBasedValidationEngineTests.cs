using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Application.Contracts.Validation;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Validation;
using DataverseMigrationTool.Infrastructure.Validation;

namespace DataverseMigrationTool.Infrastructure.Tests.Validation;

public sealed class RuleBasedValidationEngineTests
{
    [Fact]
    public async Task ValidateAsyncAggregatesFindingsFromInjectedRules()
    {
        RuleBasedValidationEngine engine = new(
        [
            new FixedRule("DMT-TEST-001", "Connectivity", ValidationSeverity.Warning),
            new FixedRule("DMT-TEST-002", "Metadata", ValidationSeverity.Info)
        ]);

        ValidationReport report = await engine.ValidateAsync(CreateJob());

        Assert.True(report.Passed);
        Assert.Equal(2, report.Counts.Total);
        Assert.Equal(1, report.Counts.Warnings);
        Assert.Equal(1, report.Counts.Infos);
    }

    [Fact]
    public async Task ValidateAsyncFailsWhenRuleProducesBlocker()
    {
        RuleBasedValidationEngine engine = new(
        [
            new FixedRule("DMT-TEST-001", "Connectivity", ValidationSeverity.Blocker)
        ]);

        ValidationReport report = await engine.ValidateAsync(CreateJob());

        Assert.True(report.Failed);
        Assert.Single(report.Blockers);
    }

    [Fact]
    public async Task ConnectivityRuleConvertsProviderErrorsToBlockersAndWarningsToWarnings()
    {
        FakeDataverseProvider provider = new()
        {
            Results =
            {
                ["Source"] = new MigrationValidationResult(false, ["Source is unreachable."], ["Source warning."]),
                ["Target"] = MigrationValidationResult.Success
            }
        };
        RuleBasedValidationEngine engine = new([new DataverseConnectivityValidationRule(provider)]);

        ValidationReport report = await engine.ValidateAsync(CreateJob());

        Assert.True(report.Failed);
        ValidationFinding blocker = Assert.Single(report.Blockers);
        ValidationFinding warning = Assert.Single(report.Warnings);
        Assert.Equal("DMT-CONNECTIVITY-001", blocker.RuleId);
        Assert.Equal("source:Source", blocker.Target);
        Assert.Equal("Source is unreachable.", blocker.Message);
        Assert.Equal("Source warning.", warning.Message);
    }

    private static MigrationJob CreateJob() => new(
        Guid.NewGuid(),
        CreateEnvironment("Source"),
        CreateEnvironment("Target"),
        ComponentSelection.Empty,
        MigrationMode.Full);

    private static EnvironmentProfile CreateEnvironment(string name) => new(
        name,
        new Uri($"https://{name.ToLowerInvariant()}.crm.dynamics.com"),
        Guid.NewGuid(),
        DataverseCloud.Public);

    private sealed class FixedRule(
        string ruleId,
        string category,
        ValidationSeverity severity) : IValidationRule
    {
        public string RuleId => ruleId;

        public string Category => category;

        public Task<IReadOnlyCollection<ValidationFinding>> EvaluateAsync(
            ValidationContext context,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ValidationFinding> findings =
            [
                new ValidationFinding(RuleId, $"{Category} finding.", severity, Category)
            ];

            return Task.FromResult(findings);
        }
    }

    private sealed class FakeDataverseProvider : IDataverseProvider
    {
        public Dictionary<string, MigrationValidationResult> Results { get; } = [];

        public Task<DataverseConnectionSession> ConnectAsync(
            EnvironmentProfile environment,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DataverseWhoAmIResult> WhoAmIAsync(
            DataverseConnectionSession session,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DataverseConnectivityCheckResult> CheckConnectivityAsync(
            EnvironmentProfile environment,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MigrationValidationResult> ValidateConnectionAsync(
            EnvironmentProfile environment,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Results.GetValueOrDefault(environment.Name, MigrationValidationResult.Success));
    }
}
