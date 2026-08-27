using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Hosting.Options;
using Storage.Cluster.Impl;

namespace Storage.Cluster;

public static class NodeInstaller
{
    public static IServiceCollection AddNodeStorage(this IServiceCollection services, IConfigurationRoot configuration) =>
        services
            .BindOptions<StorageOptions>(configuration)
            .AddSingleton<INodeStorage, NodeStorage>();
}