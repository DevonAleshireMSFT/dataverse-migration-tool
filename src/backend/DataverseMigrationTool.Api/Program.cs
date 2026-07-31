using DataverseMigrationTool.Application.Contracts;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "DataverseMigrationTool.Api"
}))
.WithName("Health");

RouteGroupBuilder migrations = app.MapGroup("/api/migration-jobs");

migrations.MapPost("/", async (
    CreateMigrationJobRequest request,
    IMigrationEngine migrationEngine,
    CancellationToken cancellationToken) =>
{
    DataverseMigrationTool.Domain.Entities.MigrationJob job =
        await migrationEngine.CreateJobAsync(request, cancellationToken);

    return Results.Created(
        $"/api/migration-jobs/{job.Id}",
        job);
})
.WithName("CreateMigrationJob");

migrations.MapGet("/{jobId:guid}", async (
    Guid jobId,
    IMigrationEngine migrationEngine,
    CancellationToken cancellationToken) =>
{
    DataverseMigrationTool.Domain.Entities.MigrationJob? job =
        await migrationEngine.GetJobAsync(jobId, cancellationToken);

    return job is null ? Results.NotFound() : Results.Ok(job);
})
.WithName("GetMigrationJob");

app.Run();
