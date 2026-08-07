namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record RollbackGuidance
{
    public RollbackGuidance(
        Guid guidanceId,
        Guid jobId,
        Guid runId,
        DateTimeOffset generatedAt,
        string summary,
        IReadOnlyList<RollbackAction> actions,
        IReadOnlyList<RollbackArtifactReference> artifactReferences)
    {
        GuidanceId = guidanceId == Guid.Empty ? throw new ArgumentException("Guidance id must not be empty.", nameof(guidanceId)) : guidanceId;
        JobId = jobId == Guid.Empty ? throw new ArgumentException("Job id must not be empty.", nameof(jobId)) : jobId;
        RunId = runId == Guid.Empty ? throw new ArgumentException("Run id must not be empty.", nameof(runId)) : runId;
        GeneratedAt = generatedAt;
        Summary = string.IsNullOrWhiteSpace(summary)
            ? throw new ArgumentException("Rollback guidance summary must not be empty.", nameof(summary))
            : summary;
        Actions = actions?.ToArray() ?? throw new ArgumentNullException(nameof(actions));
        ArtifactReferences = artifactReferences?.ToArray() ?? throw new ArgumentNullException(nameof(artifactReferences));
    }

    public Guid GuidanceId { get; }

    public Guid JobId { get; }

    public Guid RunId { get; }

    public DateTimeOffset GeneratedAt { get; }

    public string Summary { get; }

    public IReadOnlyList<RollbackAction> Actions { get; }

    public IReadOnlyList<RollbackArtifactReference> ArtifactReferences { get; }

    public RollbackGuidanceCounts Counts => new(
        ReversibleViaSupportedApi: Actions.Count(action => action.Reversibility == RollbackReversibility.ReversibleViaSupportedApi),
        ConditionallyReversible: Actions.Count(action => action.Reversibility == RollbackReversibility.ConditionallyReversible),
        RequiresManualRecovery: Actions.Count(action => action.Reversibility == RollbackReversibility.RequiresManualRecovery),
        Irreversible: Actions.Count(action => action.Reversibility == RollbackReversibility.Irreversible));
}

public sealed record RollbackAction(
    string TableLogicalName,
    Guid SourceRecordId,
    Guid? TargetRecordId,
    MigrationCheckpointUnitStatus CheckpointStatus,
    MigrationRecordWriteDisposition Disposition,
    RollbackReversibility Reversibility,
    string SupportedApiOperation,
    string RecommendedOperatorAction,
    IReadOnlyList<RollbackArtifactReference> ArtifactReferences);

public sealed record RollbackArtifactReference(
    RollbackArtifactKind Kind,
    string Identifier,
    string Description);

public sealed record RollbackGuidanceCounts(
    int ReversibleViaSupportedApi,
    int ConditionallyReversible,
    int RequiresManualRecovery,
    int Irreversible);

public enum RollbackReversibility
{
    ReversibleViaSupportedApi,
    ConditionallyReversible,
    RequiresManualRecovery,
    Irreversible
}

public enum RollbackArtifactKind
{
    RunState,
    Checkpoint,
    OperationLog,
    ValidationReport
}
