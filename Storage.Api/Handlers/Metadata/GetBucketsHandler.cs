using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Cluster;
using Storage.Cluster.DataAccess;

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
        [FromServices] IClusterDataAccess clusterDataAccess,
        CancellationToken token)
    {
        var buckets = await clusterDataAccess.GetBucketsAsync(token);
        return TypedResults.Ok(buckets
            .Select(x => x.ToDto())
            .ToList());
    }
}