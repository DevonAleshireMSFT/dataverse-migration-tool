using System.Collections.ObjectModel;
using DataverseMigrationTool.Domain.Entities;

namespace DataverseMigrationTool.Application.Contracts.Validation;

public sealed record ValidationContext
{
    public ValidationContext(
        MigrationJob job,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Job = job ?? throw new ArgumentNullException(nameof(job));
        Metadata = metadata is null
            ? ReadOnlyDictionary<string, string>.Empty
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(metadata));
    }

    public MigrationJob Job { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public static ValidationContext ForJob(MigrationJob job) => new(job);
}
