using JetBrains.Annotations;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Api.Exceptions;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers.Bucket;

[UsedImplicitly]
internal sealed class GetBucketHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/bucket/{bucketId}", GetBucketsAsync)
            .WithName("GetBucket")
            .WithTags("Bucket")
    ];

    /// <summary>Бакет по его ИД.</summary>
    /// <param name="bucketId">ИД бакета.</param>
    private static async Task<Ok<BucketDto>> GetBucketsAsync(
        [FromRoute] string bucketId,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var bucket = await clusterConnection.Buckets
            .Where(x => x.BucketId == bucketId)
            .Select(x => new BucketDto(
                x.BucketId,
                x.NodeId,
                x.Ttl))
            .SingleOrDefaultAsync(token);
        return bucket != null
            ? TypedResults.Ok(bucket)
            : throw new BucketNotFoundException(bucketId);
    }
}