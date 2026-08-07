using DataverseMigrationTool.Application.Contracts.Compare;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects.Compare;

namespace DataverseMigrationTool.Api;

public static class CompareEndpoints
{
    public static void MapCompareEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder compare = app.MapGroup("/api/compare");

        compare.MapPost("/", async (
            EnvironmentComparisonRequest request,
            IEnvironmentComparisonService comparisonService,
            CancellationToken cancellationToken) =>
        {
            EnvironmentComparisonReport report = await comparisonService.CompareAsync(request, cancellationToken);

            return Results.Ok(report);
        })
        .WithName("CompareEnvironments");
    }
}
