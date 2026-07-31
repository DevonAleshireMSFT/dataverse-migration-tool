namespace DataverseMigrationTool.Domain.Tests;

using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

public class MigrationJobTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesDraftJob()
    {
        EnvironmentProfile source = new("Source", new Uri("https://source.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public);
        EnvironmentProfile target = new("Target", new Uri("https://target.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public);

        MigrationJob job = new(Guid.NewGuid(), source, target, ComponentSelection.Empty, MigrationMode.Full);

        Assert.Equal(MigrationJobStatus.Draft, job.Status);
        Assert.Equal(source, job.Source);
        Assert.Equal(target, job.Target);
    }
}
