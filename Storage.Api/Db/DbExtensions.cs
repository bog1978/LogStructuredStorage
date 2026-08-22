using LinqToDB;
using LinqToDB.Data;
using MinimalApi.Hosting.Options;
using Storage.Db.Cluster;

namespace Storage.Api.Db;

internal static class DbExtensions
{
    internal static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetOptions<ApiOptions>();
        return services
            .AddStorage<ClusterConnection>(options.ClusterConnectionString);
    }

    private static IServiceCollection AddStorage<TStorage>(this IServiceCollection services, string connectionString)
        where TStorage : DataConnection
    {
        services
            .AddSingleton(sp => new DataOptions<TStorage>(
                new DataOptions()
                    // Если не указать диалект, то необходимо передать строку подключения для автоопределения.
                    .UsePostgreSQL(connectionString)))
            // Временем жизни подключения к БД управляет DI-контейнер.
            .AddScoped<TStorage>();

        return services;
    }
}