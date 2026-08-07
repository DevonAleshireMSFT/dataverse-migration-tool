using DataverseMigrationTool.Application.Contracts.Compare;
using DataverseMigrationTool.Domain.ValueObjects.Compare;

namespace DataverseMigrationTool.Application.Ports;

public interface IEnvironmentComparisonService
{
    Task<EnvironmentComparisonReport> CompareAsync(
        EnvironmentComparisonRequest request,
        CancellationToken cancellationToken = default);
}
