using LinqToDB;
using LinqToDB.Data;
using Storage.Api.DataAccess;
using Storage.Api.Lss;
using Storage.Api.Options;
using Storage.Api.Services;
using Storage.Cluster.DataAccess.Model;

namespace Storage.Api;

internal static class StorageInstaller
{
    public static IServiceCollection AddCluster(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddStorage(configuration)
            .AddTransient<NodeInitializer>()
            .AddTransient<DatabaseInitializer>()
            .AddHostedService<PolicyService>()
            .BindOptions<StorageOptions>(configuration)
            .AddSingleton<INodeStorage, NodeStorage>();
    }
    
    public static async Task<T> UseClusterAsync<T>(this T host)
        where T : IHost
    {
        using var scope = host.Services.CreateScope();

        var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        dbInitializer.InitializeAsync();
        
        var nodeInitializer = scope.ServiceProvider.GetRequiredService<NodeInitializer>();
        await nodeInitializer.InitializeAsync(CancellationToken.None);
        
        return host;
    }

    private static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetOptions<StorageOptions>();
        return services
            .AddStorage<ClusterConnection>(options.ConnectionString)
            .AddScoped<IClusterDataAccess, ClusterDataAccess>();
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