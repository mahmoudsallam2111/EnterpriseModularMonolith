using BuildingBlocks.Auditing;
using BuildingBlocks.Auditing.Endpoints;
using BuildingBlocks.FileStorage;
using BuildingBlocks.FileStorage.Endpoints;
using BuildingBlocks.Observability;
using BuildingBlocks.Presentation.Middleware;
using EnterpriseModularMonolith.Api.Composition;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

// ─────────────────────────────────────────────────────────────────────────────
// Bootstrap logger so failures during startup get captured.
// ─────────────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseEnterpriseSerilog();
    builder.Services.Configure<HostOptions>(options =>
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

    // ── Platform-wide services ──────────────────────────────────────────────
    builder.Services.AddPlatform(builder.Configuration);
    builder.Services.AddEnterpriseTelemetry(builder.Configuration, serviceName: "EnterpriseModularMonolith");

    // ── CORS for Angular dev server ──────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ── Authentication / Authorization ──────────────────────────────────────
    builder.Services.AddPlatformAuthentication(builder.Configuration);

    // ── Auditing (separate AuditDb, channel-backed writer) ─────────────────
    builder.Services.AddAuditing(builder.Configuration);

    // ── File storage (Local | S3 | AzureBlob — see "FileStorage:Provider") ─
    builder.Services.AddFileStorage(builder.Configuration);

    // ── Every business module composes its own services here ───────────────
    foreach (var module in ModuleRegistry.All)
        module.AddServices(builder.Services, builder.Configuration);

    // ── Health checks ───────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<Customers.Infrastructure.Persistence.CustomersDbContext>("customers-db")
        .AddDbContextCheck<Orders.Infrastructure.Persistence.OrdersDbContext>("orders-db")
        .AddDbContextCheck<BuildingBlocks.Auditing.Persistence.AuditDbContext>("audit-db");

    // ── OpenAPI ─────────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(o =>
    {
        o.SwaggerDoc("v1", new() { Title = "Enterprise Modular Monolith", Version = "v1" });
        o.OperationFilter<FileUploadOperationFilter>();
        o.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header
        });
        o.AddSecurityRequirement(new()
        {
            [new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
    });

    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // ── Pipeline ───────────────────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    // Audit scope wraps the request AFTER auth so ICurrentUser is populated, but
    // BEFORE the UoW middleware so commits happen inside the audit window.
    app.UseAuditing();

    app.UseMiddleware<UnitOfWorkMiddleware>();

    // ── Endpoints ─────────────────────────────────────────────────────────
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
            });
        }
    });

    app.MapModuleEndpoints();
    app.MapAuditEndpoints();
    app.MapFileStorageEndpoints();

    if (app.Environment.IsDevelopment()) // for testing
    {
        app.MapDevAuthEndpoints(builder.Configuration);
    }

    // ── Migrate + seed ────────────────────────────────────────────────────
    if (builder.Configuration.GetValue<bool>("Migrations:RunOnStartup"))
        await app.Services.MigrateAndSeedAsync();

    app.Run();
}
catch (Exception ex)
    when (ex.GetType().FullName == "Microsoft.Extensions.Hosting.HostAbortedException")
{
    // EF Core design-time tooling intentionally aborts the host after service discovery.
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed for integration tests via WebApplicationFactory.</summary>
public partial class Program;
