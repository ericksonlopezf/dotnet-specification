// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace EricksonLopez.Specification.PostgreSql.IntegrationTests;

[CollectionDefinition("PostgreSqlDatabase", DisableParallelization = true)]
public class PostgreSqlDatabaseCollection : ICollectionFixture<PostgreSqlFixture>
{
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    static PostgreSqlFixture()
    {
        global::Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=spec_integration_tests;Username=postgres;Password=postgres;Timeout=3;CommandTimeout=3";

    private PostgreSqlContainer? _container;
    private string _connectionString = DefaultConnectionString;

    public string ConnectionString => _connectionString;
    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        var skip = Environment.GetEnvironmentVariable("SPEC_SKIP_INTEGRATION_TESTS");
        if (string.Equals(skip, "true", StringComparison.OrdinalIgnoreCase) || skip == "1")
        {
            IsAvailable = false;
            return;
        }

        var envConn = Environment.GetEnvironmentVariable("SPEC_INTEGRATION_PG_CONNSTR");
        if (!string.IsNullOrWhiteSpace(envConn))
        {
            _connectionString = envConn;
        }
        else
        {
            try
            {
                // Probe local connection first for fast developer workflow if PostgreSQL is already running
                await using var probe = new NpgsqlConnection(DefaultConnectionString);
                await probe.OpenAsync().ConfigureAwait(false);
                _connectionString = DefaultConnectionString;
            }
            catch
            {
                try
                {
                    // Fallback to dynamic Testcontainers instance
                    _container = new PostgreSqlBuilder("postgres:15-alpine")
                        .WithDatabase("spec_integration_tests")
                        .WithUsername("postgres")
                        .WithPassword("postgres")
                        .Build();

                    await _container.StartAsync().ConfigureAwait(false);
                    _connectionString = _container.GetConnectionString();
                }
                catch (Exception)
                {
                    IsAvailable = false;
                    return;
                }
            }
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await SetupSchemaAsync(connection).ConfigureAwait(false);
            IsAvailable = true;
        }
        catch (Exception)
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<NpgsqlConnection?> CreateConnectionAsync()
    {
        if (!IsAvailable)
        {
            return null;
        }

        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        return connection;
    }

    private static async Task SetupSchemaAsync(DbConnection connection)
    {
        await connection.ExecuteAsync(@"
            DROP TABLE IF EXISTS ""Customers"" CASCADE;

            CREATE TABLE ""Customers"" (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true,
                total_purchases DECIMAL(18,2) NOT NULL DEFAULT 0,
                region TEXT NOT NULL DEFAULT 'US',
                email TEXT
            );

            INSERT INTO ""Customers"" (name, is_active, total_purchases, region, email) VALUES
                ('Alice', true, 1500.00, 'US', 'alice@example.com'),
                ('Bob', false, 250.00, 'EU', 'bob@example.com'),
                ('Charlie', true, 3200.00, 'US', null),
                ('Dave', true, 800.00, 'EU', 'dave@example.com'),
                ('Eve', true, 5000.00, 'US', null);
        ").ConfigureAwait(false);
    }
}




