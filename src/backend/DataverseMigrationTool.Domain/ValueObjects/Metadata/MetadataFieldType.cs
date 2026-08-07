namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// Defines the field data shape needed by comparison, validation, and UI consumers.
/// </summary>
public enum MetadataFieldType
{
    Unknown,
    String,
    Memo,
    Integer,
    BigInt,
    Decimal,
    Double,
    Money,
    Boolean,
    DateTime,
    Lookup,
    Customer,
    Owner,
    Picklist,
    State,
    Status,
    MultiSelectPicklist,
    UniqueIdentifier,
    EntityName,
    Image,
    File
}
