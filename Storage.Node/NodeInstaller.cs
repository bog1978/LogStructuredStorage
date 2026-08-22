using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Hosting.Options;
using Storage.Node.Impl;

namespace Storage.Node;

public static class NodeInstaller
{
    public static IServiceCollection AddNodeStorage(this IServiceCollection services, IConfigurationRoot configuration) =>
        services
            .BindOptions<StorageOptions>(configuration)
            .AddSingleton<INodeStorage, NodeStorage>();
}