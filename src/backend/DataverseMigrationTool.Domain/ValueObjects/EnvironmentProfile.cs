using DataverseMigrationTool.Domain.Enums;

namespace DataverseMigrationTool.Domain.ValueObjects;

/// <summary>
/// Identifies a Dataverse environment selected for migration work.
/// </summary>
/// <param name="Name">The operator-friendly environment name.</param>
/// <param name="Url">The absolute HTTPS Dataverse environment URL.</param>
/// <param name="TenantId">The Microsoft Entra tenant identifier.</param>
/// <param name="Cloud">The Dataverse cloud where the environment runs.</param>
public sealed record EnvironmentProfile(
    string Name,
    Uri Url,
    Guid TenantId,
    DataverseCloud Cloud);
