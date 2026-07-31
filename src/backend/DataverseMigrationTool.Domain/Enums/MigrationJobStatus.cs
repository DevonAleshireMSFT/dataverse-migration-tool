namespace DataverseMigrationTool.Domain.Enums;

public enum MigrationJobStatus
{
    Draft,
    Validating,
    Ready,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

