// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

[CollectionDefinition("MongoDbCollection", DisableParallelization = true)]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
{
}

public sealed class MongoDbFixture : IAsyncLifetime
{
    private const string DefaultConnectionString = "mongodb://localhost:27017";

    private MongoDbContainer? _mongoDbContainer;
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

        var envConn = Environment.GetEnvironmentVariable("SPEC_INTEGRATION_MONGO_CONNSTR");
        if (!string.IsNullOrWhiteSpace(envConn))
        {
            _connectionString = envConn;
        }
        else
        {
            try
            {
                // Probe local MongoDB instance first
                var probeClient = new MongoClient(new MongoClientSettings
                {
                    Server = new MongoServerAddress("localhost", 27017),
                    ServerSelectionTimeout = TimeSpan.FromSeconds(1),
                    ConnectTimeout = TimeSpan.FromSeconds(1)
                });
                await probeClient.ListDatabaseNamesAsync().ConfigureAwait(false);
                _connectionString = DefaultConnectionString;
            }
            catch
            {
                try
                {
                    _mongoDbContainer = new MongoDbBuilder("mongo:6.0")
                        .Build();

                    await _mongoDbContainer.StartAsync().ConfigureAwait(false);
                    _connectionString = _mongoDbContainer.GetConnectionString();
                }
                catch
                {
                    IsAvailable = false;
                    return;
                }
            }
        }

        try
        {
            var client = new MongoClient(_connectionString);
            await client.ListDatabaseNamesAsync().ConfigureAwait(false);
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_mongoDbContainer is not null)
        {
            try
            {
                await _mongoDbContainer.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore exceptions during cleanup to avoid failing the test suite
            }
        }
    }
}
