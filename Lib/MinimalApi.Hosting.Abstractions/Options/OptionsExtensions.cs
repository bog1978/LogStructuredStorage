using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
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

    public static TOptions GetOptions<TOptions>(this IConfiguration configuration)
        where TOptions : class, IOptionsBase
    {
        var section = configuration.GetSection(TOptions.SectionName);
        if (!section.Exists())
            throw new InvalidOperationException(
                $"Раздел конфигурации [{TOptions.SectionName}] отсутствует.");

        var options =
            section.Get<TOptions>()
            ?? throw new InvalidOperationException(
                $"Не удалось привязать секцию [{TOptions.SectionName}] " +
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