using JetBrains.Annotations;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers.Metadata;

[UsedImplicitly]
internal sealed class GetBucketsHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/bucket/", GetBucketsAsync)
            .WithName("GetBuckets")
            .WithTags("Metadata")
    ];

    /// <summary>Список корзин в кластере.</summary>
    private static async Task<Ok<List<BucketDto>>> GetBucketsAsync(
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var buckets = await clusterConnection.Buckets
            .Select(x => x.ToDto())
            .ToListAsync(token);
        return TypedResults.Ok(buckets);
    }
}