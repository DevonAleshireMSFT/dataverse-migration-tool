namespace DataverseMigrationTool.Domain.ValueObjects.Validation;

public sealed record ValidationFinding
{
    public ValidationFinding(
        string ruleId,
        string message,
        ValidationSeverity severity,
        string category,
        string? target = null)
    {
        RuleId = string.IsNullOrWhiteSpace(ruleId)
            ? throw new ArgumentException("Validation rule id must not be empty.", nameof(ruleId))
            : ruleId;
        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("Validation finding message must not be empty.", nameof(message))
            : message;
        Severity = severity;
        Category = string.IsNullOrWhiteSpace(category)
            ? throw new ArgumentException("Validation finding category must not be empty.", nameof(category))
            : category;
        Target = string.IsNullOrWhiteSpace(target) ? null : target;
    }

    public string RuleId { get; }

    public string Code => RuleId;

    public string Message { get; }

    public ValidationSeverity Severity { get; }

    public string Category { get; }

    public string? Target { get; }
}
