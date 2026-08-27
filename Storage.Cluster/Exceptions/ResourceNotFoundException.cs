using System.Runtime.CompilerServices;

namespace Storage.Cluster.Exceptions;

public abstract class ResourceNotFoundException([InterpolatedStringHandlerArgument] ref ExceptionMessageInterpolatedStringHandler handler)
    : ProblemDetailsException(ref handler)
{
    public sealed override int StatusCode => 404;
    public sealed override string StatusDescription => "Ресурс не найден.";
}