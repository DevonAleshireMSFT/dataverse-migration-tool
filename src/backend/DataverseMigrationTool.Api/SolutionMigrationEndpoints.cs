using DataverseMigrationTool.Application.Contracts.Solutions;
using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Api;

public static class SolutionMigrationEndpoints
{
    public static void MapSolutionMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder solutions = app.MapGroup("/api/solution-migrations");

        solutions.MapPost("/", async (
            SolutionMigrationRequest request,
            ISolutionExportImportOrchestrator orchestrator,
            CancellationToken cancellationToken) =>
        {
            SolutionMigrationRun run = await orchestrator.StartAsync(request, cancellationToken);
            return Results.Created($"/api/solution-migrations/{run.Id}", run);
        })
        .WithName("StartSolutionMigration");

        solutions.MapGet("/{migrationId:guid}", async (
            Guid migrationId,
            ISolutionExportImportOrchestrator orchestrator,
            CancellationToken cancellationToken) =>
        {
            SolutionMigrationRun? run = await orchestrator.GetAsync(migrationId, cancellationToken);
            return run is null ? Results.NotFound() : Results.Ok(run);
        })
        .WithName("GetSolutionMigration");
    }
}
