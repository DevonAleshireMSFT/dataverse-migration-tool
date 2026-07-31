namespace DataverseMigrationTool.Application.Tests;

using DataverseMigrationTool.Application.Contracts;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

public class CreateMigrationJobRequestTests
{
    [Fact]
    public void Constructor_PreservesSelectedMigrationMode()
    {
        EnvironmentProfile source = new("Source", new Uri("https://source.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public);
        EnvironmentProfile target = new("Target", new Uri("https://target.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public);

        CreateMigrationJobRequest request = new(source, target, ComponentSelection.Empty, MigrationMode.Incremental);

        Assert.Equal(MigrationMode.Incremental, request.Mode);
    }
}
