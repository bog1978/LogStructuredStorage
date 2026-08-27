using System.Runtime.CompilerServices;
using Storage.Api.Internal;

namespace Storage.Api.Exceptions;

/// <summary>
/// Базовый тип исключений, которые должны быть преобразованы
/// в ProblemDetails перед отправкой результата клиенту. 
/// </summary>
public abstract class ProblemDetailsException([InterpolatedStringHandlerArgument] ref ExceptionMessageInterpolatedStringHandler handler)
    : LoggableException(ref handler)
{
    /// <summary>
    /// Статус HTTP ответа. 
    /// </summary>
    public abstract int StatusCode { get; }
    
    /// <summary>
    /// Строковое описание статуса HTTP ответа.
    /// </summary>
    public abstract string StatusDescription { get; }
}