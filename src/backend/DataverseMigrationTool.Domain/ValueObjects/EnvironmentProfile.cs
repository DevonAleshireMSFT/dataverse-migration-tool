using DataverseMigrationTool.Domain.Enums;

namespace DataverseMigrationTool.Domain.ValueObjects;

public sealed record EnvironmentProfile(
    string Name,
    Uri Url,
    Guid TenantId,
    DataverseCloud Cloud);

