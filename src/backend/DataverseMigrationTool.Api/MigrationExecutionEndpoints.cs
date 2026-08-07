using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Api;

public static class MigrationExecutionEndpoints
{
    public static void MapMigrationExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder migrations = app.MapGroup("/api/migration-jobs");

        migrations.MapPost("/{jobId:guid}/execute", async (
            Guid jobId,
            IMigrationJobStore jobStore,
            IMigrationExecutor migrationExecutor,
            CancellationToken cancellationToken) =>
        {
            DataverseMigrationTool.Domain.Entities.MigrationJob? job = await jobStore.FindAsync(jobId, cancellationToken);
            if (job is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await migrationExecutor.ExecuteAsync(job, cancellationToken: cancellationToken));
        })
        .WithName("ExecuteMigrationJob");

        migrations.MapPost("/{jobId:guid}/resume", async (
            Guid jobId,
            IMigrationJobStore jobStore,
            IMigrationExecutor migrationExecutor,
            CancellationToken cancellationToken) =>
        {
            DataverseMigrationTool.Domain.Entities.MigrationJob? job = await jobStore.FindAsync(jobId, cancellationToken);
            if (job is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await migrationExecutor.ResumeAsync(job, cancellationToken: cancellationToken));
        })
        .WithName("ResumeMigrationJob");

        migrations.MapGet("/{jobId:guid}/run", async (
            Guid jobId,
            IMigrationRunStore runStore,
            CancellationToken cancellationToken) =>
        {
            DataverseMigrationTool.Application.Contracts.Migration.MigrationRun? run =
                await runStore.FindLatestForJobAsync(jobId, cancellationToken);

            return run is null ? Results.NotFound() : Results.Ok(run);
        })
        .WithName("GetMigrationRun");
    }
}
