using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Contracts;

public sealed record CreateMigrationJobRequest(
    EnvironmentProfile Source,
    EnvironmentProfile Target,
    ComponentSelection Selection,
    MigrationMode Mode);

