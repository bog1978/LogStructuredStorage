using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Storage.Api.Dto;
using Storage.Api.Internal;
using Storage.Cluster;
using Storage.Cluster.DataAccess;
using Storage.Cluster.Exceptions;

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
        [FromServices] IClusterDataAccess clusterDataAccess,
        CancellationToken token)
    {
        var bucket = await clusterDataAccess.GetBucketAsync(bucketId, token);
        return bucket != null
            ? TypedResults.Ok(bucket.ToDto())
            : throw new BucketNotFoundException(bucketId);
    }
}