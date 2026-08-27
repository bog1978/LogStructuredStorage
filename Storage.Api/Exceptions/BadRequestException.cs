using System.Runtime.CompilerServices;
using Storage.Api.Internal;

namespace Storage.Api.Exceptions;

public abstract class BadRequestException([InterpolatedStringHandlerArgument] ref ExceptionMessageInterpolatedStringHandler handler)
    : ProblemDetailsException(ref handler)
{
    public sealed override int StatusCode => 400;
    
    public sealed override string StatusDescription => "Некорректный запрос.";
}