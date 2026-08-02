using JetBrains.Annotations;
using LinqToDB;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Db;
using Storage.Api.Dto;
using Storage.Api.Exceptions;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers.Bucket;

[UsedImplicitly]
internal sealed class UpdateBucketHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapPatch("/bucket/{buckedId}", UpdateBucketsAsync)
            .WithName("UpdateBucket")
            .WithTags("Bucket")
    ];

    /// <summary>Создание нового бакета.</summary>
    private static async Task<Ok<BucketDto>> UpdateBucketsAsync(
        [FromRoute] string buckedId,
        [FromBody] BucketPatchDto patchDto,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var updatedList = await clusterConnection.Buckets
            .Where(x => x.BucketId == buckedId)
            .AsUpdatable()
            .SetIf(patchDto.NodeId != null, x => x.NodeId, () => patchDto.NodeId)
            .SetIf(patchDto.TimeToLive != null, x => x.Ttl, () => patchDto.TimeToLive)
            .UpdateWithOutputAsync((del, ins) => ins)
            .ToListAsync(token);

        var updated = updatedList.SingleOrDefault()
                      ?? throw new BucketNotFoundException(buckedId);
        
        return TypedResults.Ok(
            new BucketDto(
                updated.BucketId,
                updated.NodeId,
                updated.Ttl));
    }
}