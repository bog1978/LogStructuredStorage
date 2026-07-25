#pragma warning disable CA2254

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MinimalApi.Hosting.Exceptions;

/// <summary>
/// Базовый тип исключений с поддержкой структурного лога.
/// </summary>
public abstract class LoggableException : Exception
{
    private readonly string _template;
    private readonly object?[] _args;
    
    /// <summary>
    /// Конструктор принимает ТОЛЬКО интерполированные строки.
    /// </summary>
    protected LoggableException([InterpolatedStringHandlerArgument] ref ExceptionMessageInterpolatedStringHandler handler)
        : base(handler.Message)
    {
        _template = handler.Template;
        _args = handler.Arguments.ToArray();
    }

    /// <summary>
    /// Метод записи себя в структурный лог.
    /// </summary>
    /// <param name="logger">Логер, в который нужно себя записать.</param>
    public void LogSelf(ILogger logger) => 
        logger.Log(LogLevel.Error, this, _template, _args);
}