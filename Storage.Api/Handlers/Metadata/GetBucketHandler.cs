using JetBrains.Annotations;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Api.Exceptions;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers.Metadata;

[UsedImplicitly]
internal sealed class GetBucketHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/bucket/{bucketId}", GetBucketsAsync)
            .WithName("GetBucket")
            .WithTags("Metadata")
    ];

    /// <summary>Запрос корзины по её ИД.</summary>
    /// <param name="bucketId">ИД корзины.</param>
    private static async Task<Ok<BucketDto>> GetBucketsAsync(
        [FromRoute] string bucketId,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var bucket = await clusterConnection.Buckets
            .Where(x => x.BucketId == bucketId)
            .Select(x => x.ToDto())
            .SingleOrDefaultAsync(token);
        return bucket != null
            ? TypedResults.Ok(bucket)
            : throw new BucketNotFoundException(bucketId);
    }
}