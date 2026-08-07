namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class MigrationIdMap
{
    private readonly Dictionary<(string Table, Guid SourceId), Guid> mappings = new();

    public void Record(string tableLogicalName, Guid sourceId, Guid targetId)
    {
        mappings[(Normalize(tableLogicalName), sourceId)] = targetId;
    }

    public bool TryGetTargetId(string tableLogicalName, Guid sourceId, out Guid targetId)
        => mappings.TryGetValue((Normalize(tableLogicalName), sourceId), out targetId);

    private static string Normalize(string tableLogicalName) => tableLogicalName.ToUpperInvariant();
}
