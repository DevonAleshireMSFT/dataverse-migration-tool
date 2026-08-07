using System.Text.Json;
using System.Text.Json.Serialization;
using DataverseMigrationTool.Domain.ValueObjects.Compare;

namespace DataverseMigrationTool.Application.Contracts.Compare;

public static class EnvironmentComparisonJsonExporter
{
    public static string Export(EnvironmentComparisonReport report, bool writeIndented = true)
    {
        ArgumentNullException.ThrowIfNull(report);

        return JsonSerializer.Serialize(report, CreateSerializerOptions(writeIndented));
    }

    public static EnvironmentComparisonReport? Import(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<EnvironmentComparisonReport>(
            json,
            CreateSerializerOptions(writeIndented: false));
    }

    public static JsonSerializerOptions CreateSerializerOptions(bool writeIndented = false)
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = writeIndented
        };

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
