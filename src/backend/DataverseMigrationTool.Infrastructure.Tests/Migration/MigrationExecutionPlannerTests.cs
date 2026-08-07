using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;
using DataverseMigrationTool.Infrastructure.Migration;

namespace DataverseMigrationTool.Infrastructure.Tests.Migration;

public sealed class MigrationExecutionPlannerTests
{
    [Fact]
    public void CreatePlan_orders_parent_tables_before_children()
    {
        MigrationExecutionPlanner planner = new();
        MetadataSnapshot snapshot = Snapshot([
            Table("account"),
            Table("contact", [Relationship("contact", "parentcustomerid", "account")])
        ]);

        var plan = planner.CreatePlan(Selection("contact", "account"), snapshot);

        Assert.Equal(["account", "contact"], plan.OrderedTableLogicalNames);
        Assert.Equal(["account"], plan.Tables.Single(table => table.TableLogicalName == "contact").DependsOnTables);
    }

    [Fact]
    public void CreatePlan_defers_self_referential_relationships()
    {
        MigrationExecutionPlanner planner = new();
        MetadataSnapshot snapshot = Snapshot([
            Table("account", [Relationship("account", "parentaccountid", "account")])
        ]);

        var plan = planner.CreatePlan(Selection("account"), snapshot);

        Assert.Equal(["account"], plan.OrderedTableLogicalNames);
        Assert.True(plan.Tables.Single().HasDeferredRelationshipPatches);
    }

    [Fact]
    public void CreatePlan_orders_intersect_table_after_many_to_many_sides()
    {
        MigrationExecutionPlanner planner = new();
        MetadataSnapshot snapshot = Snapshot([
            Table("account", [new RelationshipMetadata("account_contact", MetadataRelationshipType.ManyToMany, "account", null, "contact", null, "account_contact", true)]),
            Table("contact"),
            Table("account_contact")
        ]);

        var plan = planner.CreatePlan(Selection("account", "contact", "account_contact"), snapshot);

        string[] orderedTables = plan.OrderedTableLogicalNames.ToArray();
        Assert.True(Array.IndexOf(orderedTables, "account") < Array.IndexOf(orderedTables, "account_contact"));
        Assert.True(Array.IndexOf(orderedTables, "contact") < Array.IndexOf(orderedTables, "account_contact"));
    }

    private static ComponentSelection Selection(params string[] tables) => new(true, false, tables, Array.Empty<string>());

    private static MetadataSnapshot Snapshot(IReadOnlyList<TableMetadata> tables) => new(
        new EnvironmentProfile("source", new Uri("https://source.example.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public),
        new MetadataDiscoveryScope(tables.Select(table => table.LogicalName).ToArray()),
        DateTimeOffset.UtcNow,
        tables,
        Array.Empty<ChoiceMetadata>());

    private static TableMetadata Table(string logicalName, IReadOnlyList<RelationshipMetadata>? relationships = null) => new(
        logicalName,
        logicalName,
        logicalName,
        null,
        true,
        false,
        false,
        Array.Empty<FieldMetadata>(),
        relationships ?? Array.Empty<RelationshipMetadata>(),
        Array.Empty<AlternateKeyMetadata>());

    private static RelationshipMetadata Relationship(string referencingTable, string field, string referencedTable) => new(
        $"{referencingTable}_{referencedTable}",
        MetadataRelationshipType.ManyToOne,
        referencingTable,
        field,
        referencedTable,
        null,
        null,
        true);
}
