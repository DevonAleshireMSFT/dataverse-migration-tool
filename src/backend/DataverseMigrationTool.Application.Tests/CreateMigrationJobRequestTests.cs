using DataverseMigrationTool.Application.Contracts;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Tests;

public sealed class CreateMigrationJobRequestTests
{
    [Fact]
    public void Constructor_PreservesEnvironmentSelectionAndModeForApplicationBoundary()
    {
        EnvironmentProfile source = new(
            "GFIM-DEV",
            new Uri("https://gfim-dev.crm9.dynamics.com"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DataverseCloud.GccHigh);
        EnvironmentProfile target = new(
            "GFIM-TEST",
            new Uri("https://gfim-test.crm9.dynamics.com"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DataverseCloud.GccHigh);
        ComponentSelection selection = new(
            IncludeData: true,
            IncludeSolutions: false,
            TableLogicalNames: ["account", "contact"],
            SolutionUniqueNames: []);

        CreateMigrationJobRequest request = new(source, target, selection, MigrationMode.Incremental);

        Assert.Equal(source, request.Source);
        Assert.Equal(target, request.Target);
        Assert.Equal(selection, request.Selection);
        Assert.Equal(MigrationMode.Incremental, request.Mode);
        Assert.Equal(["account", "contact"], request.Selection.TableLogicalNames);
    }
}
