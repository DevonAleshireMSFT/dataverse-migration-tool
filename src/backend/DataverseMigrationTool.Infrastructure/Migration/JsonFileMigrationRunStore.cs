using System.Text.Json;
using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class JsonFileMigrationRunStore : IMigrationRunStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object gate = new();
    private readonly string filePath;
    private readonly string checkpointFilePath;
    private readonly string rollbackGuidanceFilePath;

    public JsonFileMigrationRunStore()
        : this(Path.Combine(AppContext.BaseDirectory, "migration-state", "migration-runs.json"))
    {
    }

    public JsonFileMigrationRunStore(string filePath)
    {
        this.filePath = string.IsNullOrWhiteSpace(filePath)
            ? throw new ArgumentException("Run store file path must not be empty.", nameof(filePath))
            : filePath;
        checkpointFilePath = Path.Combine(Path.GetDirectoryName(this.filePath)!, $"{Path.GetFileNameWithoutExtension(this.filePath)}-checkpoints.json");
        rollbackGuidanceFilePath = Path.Combine(Path.GetDirectoryName(this.filePath)!, $"{Path.GetFileNameWithoutExtension(this.filePath)}-rollback-guidance.json");
    }

    public Task SaveAsync(MigrationRun run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            Dictionary<Guid, MigrationRun> runs = ReadAll();
            runs[run.RunId] = run;
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, JsonSerializer.Serialize(runs.Values.OrderBy(value => value.StartedAt), SerializerOptions));
        }

        return Task.CompletedTask;
    }

    public Task<MigrationRun?> FindAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            ReadAll().TryGetValue(runId, out MigrationRun? run);
            return Task.FromResult(run);
        }
    }

    public Task<MigrationRun?> FindLatestForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            MigrationRun? run = ReadAll().Values
                .Where(candidate => candidate.JobId == jobId)
                .OrderByDescending(candidate => candidate.StartedAt)
                .FirstOrDefault();
            return Task.FromResult(run);
        }
    }

    public Task SaveCheckpointAsync(MigrationCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            Dictionary<Guid, MigrationCheckpoint> checkpoints = ReadAllCheckpoints();
            checkpoints[checkpoint.CheckpointId] = checkpoint;
            Directory.CreateDirectory(Path.GetDirectoryName(checkpointFilePath)!);
            File.WriteAllText(checkpointFilePath, JsonSerializer.Serialize(checkpoints.Values.OrderBy(value => value.UpdatedAt), SerializerOptions));

            Dictionary<Guid, MigrationRun> runs = ReadAll();
            if (runs.TryGetValue(checkpoint.RunId, out MigrationRun? run))
            {
                runs[run.RunId] = run with { Checkpoint = checkpoint, ResumeGuidance = checkpoint.ResumeGuidance, Errors = checkpoint.Errors };
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllText(filePath, JsonSerializer.Serialize(runs.Values.OrderBy(value => value.StartedAt), SerializerOptions));
            }
        }

        return Task.CompletedTask;
    }

    public Task<MigrationCheckpoint?> FindLatestCheckpointForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            MigrationCheckpoint? checkpoint = ReadAllCheckpoints().Values
                .Where(candidate => candidate.JobId == jobId)
                .OrderByDescending(candidate => candidate.Marker)
                .ThenByDescending(candidate => candidate.UpdatedAt)
                .FirstOrDefault();
            return Task.FromResult(checkpoint);
        }
    }

    public Task SaveRollbackGuidanceAsync(RollbackGuidance guidance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(guidance);
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            Dictionary<Guid, RollbackGuidance> guidanceById = ReadAllRollbackGuidance();
            guidanceById[guidance.GuidanceId] = guidance;
            Directory.CreateDirectory(Path.GetDirectoryName(rollbackGuidanceFilePath)!);
            File.WriteAllText(rollbackGuidanceFilePath, JsonSerializer.Serialize(guidanceById.Values.OrderBy(value => value.GeneratedAt), SerializerOptions));
        }

        return Task.CompletedTask;
    }

    public Task<RollbackGuidance?> FindLatestRollbackGuidanceForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            RollbackGuidance? guidance = ReadAllRollbackGuidance().Values
                .Where(candidate => candidate.JobId == jobId)
                .OrderByDescending(candidate => candidate.GeneratedAt)
                .FirstOrDefault();
            return Task.FromResult(guidance);
        }
    }

    private Dictionary<Guid, MigrationRun> ReadAll()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        string json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        MigrationRun[] runs = JsonSerializer.Deserialize<MigrationRun[]>(json, SerializerOptions) ?? [];
        return runs.ToDictionary(run => run.RunId);
    }

    private Dictionary<Guid, MigrationCheckpoint> ReadAllCheckpoints()
    {
        if (!File.Exists(checkpointFilePath))
        {
            return [];
        }

        string json = File.ReadAllText(checkpointFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        MigrationCheckpoint[] checkpoints = JsonSerializer.Deserialize<MigrationCheckpoint[]>(json, SerializerOptions) ?? [];
        return checkpoints.ToDictionary(checkpoint => checkpoint.CheckpointId);
    }

    private Dictionary<Guid, RollbackGuidance> ReadAllRollbackGuidance()
    {
        if (!File.Exists(rollbackGuidanceFilePath))
        {
            return [];
        }

        string json = File.ReadAllText(rollbackGuidanceFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        RollbackGuidance[] guidance = JsonSerializer.Deserialize<RollbackGuidance[]>(json, SerializerOptions) ?? [];
        return guidance.ToDictionary(item => item.GuidanceId);
    }
}
