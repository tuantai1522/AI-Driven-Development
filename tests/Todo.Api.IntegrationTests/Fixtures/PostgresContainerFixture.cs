using Npgsql;
using Testcontainers.PostgreSql;

namespace Todo.Api.IntegrationTests.Fixtures;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private const ushort PostgreSqlPort = 5432;
    private readonly PostgreSqlContainer _container;
    private readonly NpgsqlConnectionStringBuilder _connectionStringTemplate;

    public PostgresContainerFixture(string configuredConnectionString)
    {
        _connectionStringTemplate = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        _container = new PostgreSqlBuilder("postgres:17")
            .WithDatabase(_connectionStringTemplate.Database)
            .WithUsername(_connectionStringTemplate.Username)
            .WithPassword(_connectionStringTemplate.Password)
            .Build();
    }

    public string ConnectionString
    {
        get
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_connectionStringTemplate.ConnectionString)
            {
                Host = _container.Hostname,
                Port = _container.GetMappedPublicPort(PostgreSqlPort)
            };

            return connectionStringBuilder.ConnectionString;
        }
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
