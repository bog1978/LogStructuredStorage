namespace Storage.Api.Internal;

/// <summary>
/// Маркерный интерфейс для всех обработчиков конечных точек.
/// Каждый обработчик должен обрабатывать ровно один маршрут.
/// </summary>
public interface IEndpointHandler
{
    /// <summary>
    /// Каждый обработчик должен содержать статический метод для настройки маршрута.
    /// </summary>
    static abstract IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder);
}