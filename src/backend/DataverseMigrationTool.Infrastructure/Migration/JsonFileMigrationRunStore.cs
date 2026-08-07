using System.Text.Json;
using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class JsonFileMigrationRunStore : IMigrationRunStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object gate = new();
    private readonly string filePath;

    public JsonFileMigrationRunStore()
        : this(Path.Combine(AppContext.BaseDirectory, "migration-state", "migration-runs.json"))
    {
    }

    public JsonFileMigrationRunStore(string filePath)
    {
        this.filePath = string.IsNullOrWhiteSpace(filePath)
            ? throw new ArgumentException("Run store file path must not be empty.", nameof(filePath))
            : filePath;
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
}
