using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Domain.Tests;

public sealed class MigrationJobTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesDraftJobWithRequestedScope()
    {
        EnvironmentProfile source = CreateEnvironment("Source", "https://source.crm.dynamics.com");
        EnvironmentProfile target = CreateEnvironment("Target", "https://target.crm.dynamics.com");
        ComponentSelection selection = new(
            IncludeData: true,
            IncludeSolutions: true,
            TableLogicalNames: ["account", "contact"],
            SolutionUniqueNames: ["DataverseMigrationTool"]);

        MigrationJob job = new(Guid.NewGuid(), source, target, selection, MigrationMode.Full);

        Assert.Equal(MigrationJobStatus.Draft, job.Status);
        Assert.Equal(source, job.Source);
        Assert.Equal(target, job.Target);
        Assert.Equal(selection, job.Selection);
        Assert.Equal(MigrationMode.Full, job.Mode);
        Assert.Equal(job.CreatedAt, job.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithEmptyId_RejectsInvalidIdentity()
    {
        EnvironmentProfile source = CreateEnvironment("Source", "https://source.crm.dynamics.com");
        EnvironmentProfile target = CreateEnvironment("Target", "https://target.crm.dynamics.com");

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new MigrationJob(Guid.Empty, source, target, ComponentSelection.Empty, MigrationMode.Incremental));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void MarkStatus_WhenJobProgresses_ChangesStatusAndRefreshesUpdatedAt()
    {
        MigrationJob job = new(
            Guid.NewGuid(),
            CreateEnvironment("Source", "https://source.crm.dynamics.com"),
            CreateEnvironment("Target", "https://target.crm.dynamics.com"),
            ComponentSelection.Empty,
            MigrationMode.Incremental);
        DateTimeOffset originalUpdatedAt = job.UpdatedAt;

        job.MarkStatus(MigrationJobStatus.Running);

        Assert.Equal(MigrationJobStatus.Running, job.Status);
        Assert.True(job.UpdatedAt >= originalUpdatedAt);
    }

    private static EnvironmentProfile CreateEnvironment(string name, string url) =>
        new(name, new Uri(url), Guid.NewGuid(), DataverseCloud.Public);
}
