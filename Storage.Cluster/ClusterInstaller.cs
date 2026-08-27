using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalApi.Hosting.Options;
using Storage.Cluster.DataAccess;
using Storage.Cluster.Model;
using Storage.Cluster.Services;

namespace Storage.Cluster;

public static class ClusterInstaller
{
    public static IServiceCollection AddCluster(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .BindOptions<ClusterOptions>(configuration)
            .AddStorage(configuration)
            .AddTransient<NodeInitializer>()
            .AddTransient<DatabaseInitializer>()
            .AddHostedService<PolicyService>();
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
        var options = configuration.GetOptions<ClusterOptions>();
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