using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Storage.Client;

public static class StorageInstaller
{
    public static IServiceCollection AddStorageClient(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? builder = null, 
        RefitSettings? settings = null)
    {
        var options = configuration
            .GetSection(StorageClientOptions.SectionName)
            .Get<StorageClientOptions>()
            ?? throw new InvalidOperationException($"Не найден раздел {StorageClientOptions.SectionName} в конфигурации.");
        var httpClientBuilder = services
            .AddRefitGeneratedClient<IStorageApi>(settings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUri));
        builder?.Invoke(httpClientBuilder);
        return services;
    }
}