namespace DataverseMigrationTool.Domain.Enums;

public enum MigrationJobStatus
{
    Draft,
    Validating,
    Ready,
    Planning,
    Extracting,
    Transforming,
    Loading,
    PatchingRelationships,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}
