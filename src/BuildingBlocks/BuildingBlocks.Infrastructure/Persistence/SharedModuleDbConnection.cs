using System.Data.Common;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Scoped database connection shared by every module DbContext in the request.
/// Sharing the connection lets EF Core enlist multiple DbContexts in one transaction.
/// </summary>
public sealed class SharedModuleDbConnection : IAsyncDisposable, IDisposable
{
    private readonly string _connectionString;
    private DbConnection? _connection;

    public SharedModuleDbConnection(string connectionString) => _connectionString = connectionString;

    public DbConnection Connection => _connection ??= new NpgsqlConnection(_connectionString);

    public void Dispose() => _connection?.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}

