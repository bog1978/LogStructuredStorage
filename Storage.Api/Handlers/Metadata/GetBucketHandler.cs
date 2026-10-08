using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Storage.Api.DataAccess;
using Storage.Api.Dto;
using Storage.Api.Exceptions;
using Storage.Api.Internal;
using Storage.Api.Validation;
using Storage.Cluster;
using Storage.Cluster.DataAccess;

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
        var normalizedBucketName = BucketNameValidator.NormalizeAndValidate(bucketId);
        var bucket = await clusterDataAccess.GetBucketAsync(normalizedBucketName, token);
        return bucket != null
            ? TypedResults.Ok(bucket.ToDto())
            : throw new BucketNotFoundException(bucketId);
    }
}
