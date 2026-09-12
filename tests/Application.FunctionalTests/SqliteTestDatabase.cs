using System.Data;
using System.Data.Common;
using CleanArchitecture.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application.FunctionalTests;

public class SqliteTestDatabase : ITestDatabase
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connectionString = "DataSource=:memory:";
        _connection = new SqliteConnection(_connectionString);
    }

    public async Task InitialiseAsync()
    {
        if (_connection.State == ConnectionState.Open)
        {
            await _connection.CloseAsync();
        }

        await _connection.OpenAsync();

        // Mirrors the MaxLengthForKeys configured by AddDefaultIdentity() in the real app's
        // DI container (src/Infrastructure/DependencyInjection.cs), so that this standalone
        // context computes the same Identity key-column lengths as the app when migrating.
        var identityServices = new ServiceCollection()
            .Configure<IdentityOptions>(o => o.Stores.MaxLengthForKeys = 128)
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseApplicationServiceProvider(identityServices)
            .UseSqlite(_connection)
            .Options;

        var context = new ApplicationDbContext(options);

        context.Database.Migrate();
    }

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public async Task ResetAsync()
    {
        await InitialiseAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}