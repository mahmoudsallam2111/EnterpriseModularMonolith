using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Spins the full host on a real Postgres container so integration tests cover
/// the actual EF Core mappings, interceptors, outbox, and module wiring.
/// </summary>
//public sealed class EmmWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
//{
//    //private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
//    //    .WithImage("postgres:17-alpine")
//    //    .WithUsername("emm")
//    //    .WithPassword("emm")
//    //    .WithDatabase("emm")
//    //    .Build();

//    //public string ConnectionString => _postgres.GetConnectionString();

//    //protected override void ConfigureWebHost(IWebHostBuilder builder)
//    //{
//    //    builder.UseEnvironment("Development");
//    //    builder.ConfigureAppConfiguration((_, cfg) =>
//    //    {
//    //        cfg.AddInMemoryCollection(new Dictionary<string, string?>
//    //        {
//    //            ["ConnectionStrings:Postgres"] = ConnectionString,
//    //            ["Jwt:SigningKey"] = "test-signing-key-please-ignore-this-is-just-for-tests-0000",
//    //            ["Migrations:RunOnStartup"] = "true"
//    //        });
//    //    });
//    //}

//    //public Task InitializeAsync() => _postgres.StartAsync();
//    //public new Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
//}
