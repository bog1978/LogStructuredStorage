using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi.Hosting.Options;

public static class OptionsExtensions
{
    public static IServiceCollection BindOptions<TOptions>(
        this IServiceCollection services,
        IConfigurationRoot configuration)
        where TOptions : class, IOptionsBase =>
        BindOptions<TOptions>(services, configuration, TOptions.SectionName);

    public static IServiceCollection BindOptions<TOptions>(
        this IServiceCollection services,
        IConfigurationRoot configuration,
        string sectionName)
        where TOptions : class
    {
        var section = configuration.GetSection(sectionName);
        services
            .AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(
                _ => section.Exists(),
                $"Раздел конфигурации [{sectionName}] отсутствует.")
            .ValidateOnStart();
        return services;
    }

    public static TOptions GetOptions<TOptions>(
        this IConfiguration configuration)
        where TOptions : class, IOptionsBase =>
        configuration.GetOptions<TOptions>(TOptions.SectionName);

    public static TOptions GetOptions<TOptions>(
        this IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
            throw new InvalidOperationException(
                $"Раздел конфигурации [{sectionName}] отсутствует.");

        var options =
            section.Get<TOptions>()
            ?? throw new InvalidOperationException(
                $"Не удалось привязать секцию [{sectionName}] " +
                $"к классу настроек [{typeof(TOptions).FullName}].");

        // Валидация через DataAnnotations
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();
        if (Validator.TryValidateObject(options, validationContext,
            validationResults, validateAllProperties: true))
            return options;

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Ошибка валидации для [{typeof(TOptions).FullName}]:");
        foreach (var result in validationResults)
            sb.Append(string.Join(", ", result.MemberNames))
                .Append(": ")
                .AppendLine(result.ErrorMessage);
        throw new InvalidOperationException(sb.ToString());
    }
}