using System.Data.Common;
using Microsoft.AspNetCore.Http;
using MinimalApi.Hosting.Exceptions;

namespace MinimalApi.Hosting.Filters;

/// <summary>
/// Ловит исключения типа <see cref="ProblemDetailsException"/>, преобразует их
/// в ProblemDetails и возвращает вместо исключения. Если исключение не является
/// потомком <see cref="ProblemDetailsException"/>, то ничего не делает.
/// </summary>
internal sealed class ProblemDetailsExceptionEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ProblemDetailsException ex)
        {
            // Возвращаем как результат в формате ProblemDetails с кодом ошибки, чтобы Asp.Net не задублировал лог.
            return TypedResults.Problem(
                statusCode: ex.StatusCode,
                title: ex.StatusDescription,
                detail: ex.Message,
                instance: context.HttpContext.Request.Path.Value);
        }
        catch (DbException ex)
        {
            return TypedResults.Problem(
                statusCode: 500,
                title: "Ошибка на уровне БД.",
                detail: $"Ошибка на уровне БД: {ex.Message}",
                instance: context.HttpContext.Request.Path.Value);
        }
        // NOTE: Все остальные исключения обрабатываются Asp.Net стандартным образом.
    }
}