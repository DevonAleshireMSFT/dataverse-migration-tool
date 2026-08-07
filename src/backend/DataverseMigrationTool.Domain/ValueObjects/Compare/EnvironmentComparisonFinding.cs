using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Domain.ValueObjects.Compare;

public sealed record EnvironmentComparisonFinding
{
    public EnvironmentComparisonFinding(
        string code,
        string message,
        ValidationSeverity severity,
        ComparisonSubjectKind subjectKind,
        string subjectName,
        string? tableLogicalName = null,
        string? fieldLogicalName = null)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("Comparison finding code must not be empty.", nameof(code))
            : code;
        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("Comparison finding message must not be empty.", nameof(message))
            : message;
        Severity = severity;
        SubjectKind = subjectKind;
        SubjectName = string.IsNullOrWhiteSpace(subjectName)
            ? throw new ArgumentException("Comparison subject name must not be empty.", nameof(subjectName))
            : subjectName;
        TableLogicalName = string.IsNullOrWhiteSpace(tableLogicalName) ? null : tableLogicalName;
        FieldLogicalName = string.IsNullOrWhiteSpace(fieldLogicalName) ? null : fieldLogicalName;
    }

    public string Code { get; }

    public string Message { get; }

    public ValidationSeverity Severity { get; }

    public ComparisonSubjectKind SubjectKind { get; }

    public string SubjectName { get; }

    public string? TableLogicalName { get; }

    public string? FieldLogicalName { get; }
}
