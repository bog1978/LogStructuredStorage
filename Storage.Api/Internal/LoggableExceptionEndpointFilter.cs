using System.Data.Common;
using Storage.Api.Exceptions;

namespace Storage.Api.Internal;

/// <summary>
/// Ловит исключения типа <see cref="LoggableException"/>, логирует их и прокидывает дальше.
/// </summary>
internal sealed class LoggableExceptionEndpointFilter(ILogger<LoggableExceptionEndpointFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (LoggableException ex)
        {
            // Пишем исключение в лог и прокидываем дальше.
            ex.LogSelf(logger);
            throw;
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Ошибка на уровне БД: {Message}", ex.Message);
            throw;
        }
    }
}