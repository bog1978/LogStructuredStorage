using EvolveDb.Configuration;
using EvolveDb.Dialect;
using Microsoft.Extensions.Options;
using Npgsql;
using Storage.Api.Options;

namespace Storage.Api.Services;

internal sealed class DatabaseInitializer(ILogger<DatabaseInitializer> logger, IOptions<StorageOptions> options)
{
    private readonly StorageOptions _options = options.Value;

    public void InitializeAsync()
    {
        EnsureDbExists();

        using var connection = new NpgsqlConnection(_options.ConnectionString);
        var evolve = new EvolveDb.Evolve(
            connection,
            msg => logger.LogWarning("Миграция: {Message}", msg),
            DBMS.PostgreSQL)
        {
            EmbeddedResourceAssemblies = [GetType().Assembly],
            //Locations = ["Migrations"],
            MetadataTableSchema = "public",
            MetadataTableName = "__evolve__",
            IsEraseDisabled = true,
            // TODO: Отключить в продакшене
            MustEraseOnValidationError = true,
            TransactionMode = TransactionKind.CommitAll,
            CommandTimeout = 3600,
            AmbientTransactionTimeout = 3600,
        };
        evolve.Migrate();
    }

    private void EnsureDbExists()
    {
        var cb = new NpgsqlConnectionStringBuilder(_options.ConnectionString);
        var dbName = cb.Database ?? throw new InvalidOperationException("Не задано имя БД.");
        cb.Database = "postgres";

        using var connection = new NpgsqlConnection(cb.ToString());
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_database WHERE lower(datname)=lower(@dbname));";
        cmd.Parameters.AddWithValue("dbname", dbName);
        var exists = cmd.ExecuteScalar();
        if (exists is true)
            return;

        var createDbCmd = connection.CreateCommand();
        createDbCmd.CommandText = $"""CREATE DATABASE "{dbName}";""";
        createDbCmd.ExecuteNonQuery();
    }
}