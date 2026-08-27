using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Storage.Api.DataAccess;
using Storage.Api.Dto;
using Storage.Api.Exceptions;
using Storage.Api.Internal;
using Storage.Cluster;
using Storage.Cluster.DataAccess;

namespace Storage.Api.Handlers.Metadata;

[UsedImplicitly]
internal sealed class UpdateBucketHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapPatch("/bucket/{bucketId}", UpdateBucketsAsync)
            .WithName("UpdateBucket")
            .WithTags("Metadata")
    ];

    /// <summary>Изменение параметров корзины.</summary>
    /// <param name="bucketId">Идентификатор корзины.</param>
    /// <param name="patchDto">Новые параметры корзины.</param>
    private static async Task<Ok<BucketDto>> UpdateBucketsAsync(
        [FromRoute] string bucketId,
        [FromBody] BucketPatchDto patchDto,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        CancellationToken token)
    {
        var updated =
            await clusterDataAccess.UpdateBucketAsync(bucketId, patchDto.NodeId, patchDto.TimeToLive, token)
            ?? throw new BucketNotFoundException(bucketId);
        return TypedResults.Ok(updated.ToDto());
    }
}