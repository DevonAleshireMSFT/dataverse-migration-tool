using System.Text.Json;
using System.Text.Json.Serialization;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Application.Contracts.Validation;

public static class ValidationReportJsonExporter
{
    public static string Export(ValidationReport report, bool writeIndented = true)
    {
        ArgumentNullException.ThrowIfNull(report);

        return JsonSerializer.Serialize(report, CreateSerializerOptions(writeIndented));
    }

    public static ValidationReport Import(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<ValidationReport>(json, CreateSerializerOptions(writeIndented: false))
            ?? ValidationReport.Empty;
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
