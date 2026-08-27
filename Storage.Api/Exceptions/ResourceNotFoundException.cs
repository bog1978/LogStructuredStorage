using System.Runtime.CompilerServices;
using Storage.Api.Internal;

namespace Storage.Api.Exceptions;

public abstract class ResourceNotFoundException([InterpolatedStringHandlerArgument] ref ExceptionMessageInterpolatedStringHandler handler)
    : ProblemDetailsException(ref handler)
{
    public sealed override int StatusCode => 404;
    public sealed override string StatusDescription => "Ресурс не найден.";
}