using System.Runtime.CompilerServices;

namespace MinimalApi.Hosting.Exceptions;

public abstract class InternalServerErrorException([InterpolatedStringHandlerArgument] ref ExceptionMessageInterpolatedStringHandler handler)
    : ProblemDetailsException(ref handler)
{
    public sealed override int StatusCode => 500;
    
    public sealed override string StatusDescription => "Внутренняя ошибка сервера.";
}