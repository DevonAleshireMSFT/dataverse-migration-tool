using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Domain.Entities;

public sealed class MigrationJob
{
    public MigrationJob(
        Guid id,
        EnvironmentProfile source,
        EnvironmentProfile target,
        ComponentSelection selection,
        MigrationMode mode,
        MigrationJobStatus status = MigrationJobStatus.Draft)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Migration job id must not be empty.", nameof(id)) : id;
        Source = source;
        Target = target;
        Selection = selection;
        Mode = mode;
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; }

    public EnvironmentProfile Source { get; }

    public EnvironmentProfile Target { get; }

    public ComponentSelection Selection { get; }

    public MigrationMode Mode { get; }

    public MigrationJobStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkStatus(MigrationJobStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

