using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Hosting.Filters;
using static System.Reflection.BindingFlags;

namespace MinimalApi.Hosting;

public static class HandlerInstaller
{
    /// <summary>
    /// Добавляет обработчики конечных точек в DI-контейнер, чтобы вместо статического метода
    /// можно было воспользоваться constructor injection + instance method.
    /// </summary>
    public static IServiceCollection AddApiHandlers(this IServiceCollection services, Assembly? assembly = null)
    {
        var asm = assembly ?? Assembly.GetCallingAssembly();
        foreach (var type in GetEndpointHandlers(asm))
            services.AddScoped(type);
        return services;
    }

    /// <summary>
    /// Перебирает все типы текущей сборки, которые реализуют интерфейс <see cref="IEndpointHandler"/>, и 
    /// для каждого из них вызывает статический метод <see cref="IEndpointHandler.ConfigureEndpoint"/>,
    /// который настраивает маршруты. Затем для каждого маршрута добавляет описание в OpenAPI.
    /// </summary>    
    public static T MapApiHandlers<T>(this T app, Assembly? assembly = null)
        where T : IEndpointRouteBuilder
    {
        var asm = assembly ?? Assembly.GetCallingAssembly();
        foreach (var type in GetEndpointHandlers(asm))
        {
            var result = type.InvokeMember(
                nameof(IEndpointHandler.ConfigureEndpoint),
                Public | Static | InvokeMethod,
                null,
                null,
                [app],
                CultureInfo.InvariantCulture);
            if (result is IEndpointConventionBuilder[] routeBuilders)
            {
                // Здесь можно добавить общие настройки для всех обработчиков.
                foreach (var routeBuilder in routeBuilders)
                    routeBuilder
                        .AddEndpointFilter<IEndpointConventionBuilder, ProblemDetailsExceptionEndpointFilter>()
                        .AddEndpointFilter<IEndpointConventionBuilder, LoggableExceptionEndpointFilter>()
                        .WithOpenApi();
            }
        }

        return app;
    }

    public static T UseMaxRequestBodySize<T>(this T app, long? size)
        where T : IApplicationBuilder
    {
        app.Use(async (context, next) =>
        {
            var maxFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (maxFeature is { IsReadOnly: false })
                maxFeature.MaxRequestBodySize = size; // null = без ограничения
            await next.Invoke();
        });
        return app;
    }

    /// <summary>
    /// Возвращает последовательность типов обработчиков, которые
    /// реализуют интерфейс IEndpointHandler и находятся в текущей сборке.
    /// </summary>
    private static IEnumerable<Type> GetEndpointHandlers(Assembly assembly) =>
        assembly.GetTypes().Where(t =>
            t is { IsClass: true, IsAbstract: false }
            && t.IsAssignableTo(typeof(IEndpointHandler)));

    public static T AsDeprecated<T>(this T app)
        where T : IEndpointConventionBuilder =>
        app.WithOpenApi(o =>
        {
            o.Deprecated = true;
            return o;
        });
}