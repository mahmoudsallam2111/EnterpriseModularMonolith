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

    // ── Platform-wide services ──────────────────────────────────────────────
    builder.Services.AddPlatform(builder.Configuration);
    builder.Services.AddEnterpriseTelemetry(builder.Configuration, serviceName: "EnterpriseModularMonolith");

    // ── Authentication / Authorization ──────────────────────────────────────
    builder.Services.AddPlatformAuthentication(builder.Configuration);

    // ── Every business module composes its own services here ───────────────
    foreach (var module in ModuleRegistry.All)
        module.AddServices(builder.Services, builder.Configuration);

    // ── Health checks ───────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<Customers.Infrastructure.Persistence.CustomersDbContext>("customers-db")
        .AddDbContextCheck<Orders.Infrastructure.Persistence.OrdersDbContext>("orders-db")
        .AddDbContextCheck<Users.Infrastructure.Persistence.UsersDbContext>("users-db");

    // ── OpenAPI ─────────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(o =>
    {
        o.SwaggerDoc("v1", new() { Title = "Enterprise Modular Monolith", Version = "v1" });
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

    app.UseAuthentication();
    app.UseAuthorization();

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

    // ── Migrate + seed ────────────────────────────────────────────────────
    if (builder.Configuration.GetValue<bool>("Migrations:RunOnStartup"))
        await app.Services.MigrateAndSeedAsync();

    app.Run();
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
