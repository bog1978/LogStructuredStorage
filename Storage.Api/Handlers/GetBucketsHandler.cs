using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalApi.Hosting;

namespace Storage.Api.Handlers;

[UsedImplicitly]
internal sealed class GetBucketsHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/buckets/", GetBucketsAsync)
            .WithName("GetBuckets")
            .WithTags("Buckets")
    ];

    /// <summary>Список бакетов.</summary>
    private static async Task<Ok<List<string>>> GetBucketsAsync(
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IOptions<StorageOptions> options,
        CancellationToken token)
    {
        return TypedResults.Ok(new List<string> { "" });
    }
}