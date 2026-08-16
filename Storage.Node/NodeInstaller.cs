using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Hosting.Options;

namespace Storage.Node;

public static class NodeInstaller
{
    public static IServiceCollection AddNodeStorage(this IServiceCollection services, IConfigurationRoot configuration) =>
        services
            .BindOptions<NodeStorageOptions>(configuration)
            .AddSingleton<INodeStorage, NodeStorage>();
}