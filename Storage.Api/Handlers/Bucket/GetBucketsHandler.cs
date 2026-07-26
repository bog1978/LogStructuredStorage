using JetBrains.Annotations;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers.Bucket;

[UsedImplicitly]
internal sealed class GetBucketsHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/bucket/", GetBucketsAsync)
            .WithName("GetBuckets")
            .WithTags("Bucket")
    ];

    /// <summary>Список бакетов.</summary>
    private static async Task<Ok<List<BucketDto>>> GetBucketsAsync(
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var buckets = await clusterConnection.Buckets
            .Select(x => new BucketDto(
                x.BucketId,
                x.BucketName,
                x.NodeId,
                x.Ttl))
            .ToListAsync(token);
        return TypedResults.Ok(buckets);
    }
}