namespace DataverseMigrationTool.Application.Contracts.Dataverse;

/// <summary>
/// Result returned by the supported Dataverse WhoAmI operation.
/// </summary>
/// <param name="UserId">The Dataverse system user identifier.</param>
/// <param name="BusinessUnitId">The user's Dataverse business unit identifier.</param>
/// <param name="OrganizationId">The Dataverse organization identifier.</param>
public sealed record DataverseWhoAmIResult(
    Guid UserId,
    Guid BusinessUnitId,
    Guid OrganizationId);
