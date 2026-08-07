using System.Text.Json;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Domain.Tests.Metadata;

public sealed class MetadataReadModelTests
{
    [Fact]
    public void MetadataSnapshot_RoundTripsThroughJson()
    {
        EnvironmentProfile environment = new(
            "DEV",
            new Uri("https://org.crm.dynamics.com"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DataverseCloud.Public);

        FieldMetadata field = new(
            "name",
            "Name",
            "Name",
            "Primary name",
            MetadataFieldType.String,
            MetadataRequiredLevel.ApplicationRequired,
            IsPrimaryId: false,
            IsPrimaryName: true,
            IsValidForRead: true,
            IsValidForCreate: true,
            IsValidForUpdate: true,
            Array.Empty<string>());

        RelationshipMetadata relationship = new(
            "account_primary_contact",
            MetadataRelationshipType.ManyToOne,
            "account",
            "primarycontactid",
            "contact",
            "contactid",
            IntersectTableName: null,
            IsCustomRelationship: false);

        AlternateKeyMetadata alternateKey = new(
            "ak_accountnumber",
            "ak_accountnumber",
            "Account Number",
            ["accountnumber"],
            IsManaged: false);

        TableMetadata table = new(
            "account",
            "Account",
            "Account",
            "Account table",
            IsCustomTable: false,
            IsActivity: false,
            IsIntersect: false,
            [field],
            [relationship],
            [alternateKey]);

        ChoiceMetadata choice = new(
            "account_categorycode",
            "Category",
            ChoiceKind.Local,
            [new ChoiceOption(1, "Preferred", "Preferred customer", DisplayOrder: 10)],
            "account",
            "categorycode");

        MetadataSnapshot snapshot = new(
            environment,
            new MetadataDiscoveryScope(["account"]),
            DateTimeOffset.Parse("2026-08-06T00:00:00+00:00"),
            [table],
            [choice]);

        string json = JsonSerializer.Serialize(snapshot);
        MetadataSnapshot? roundTripped = JsonSerializer.Deserialize<MetadataSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal("account", roundTripped.Tables.Single().LogicalName);
        Assert.Equal(MetadataFieldType.String, roundTripped.Tables.Single().Fields.Single().Type);
        Assert.Equal(MetadataRelationshipType.ManyToOne, roundTripped.Tables.Single().Relationships.Single().Type);
        Assert.Equal("ak_accountnumber", roundTripped.Tables.Single().AlternateKeys.Single().LogicalName);
        Assert.Equal(1, roundTripped.Choices.Single().Options.Single().Value);
    }
}
