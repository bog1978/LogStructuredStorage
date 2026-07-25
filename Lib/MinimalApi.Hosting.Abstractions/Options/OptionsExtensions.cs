using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi.Hosting.Options;

public static class OptionsExtensions
{
    public static IServiceCollection BindOptions<TOptions>(this IServiceCollection services, IConfigurationRoot configuration)
        where TOptions : class, IOptionsBase
    {
        var section = configuration.GetSection(TOptions.SectionName);
        services
            .AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(
                _ => section.Exists(),
                $"Раздел конфигурации [{TOptions.SectionName}] отсутствует.")
            .ValidateOnStart();
        return services;
    }
}