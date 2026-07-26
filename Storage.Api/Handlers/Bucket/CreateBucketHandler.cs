using JetBrains.Annotations;
using LinqToDB;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers.Bucket;

[UsedImplicitly]
internal sealed class CreateBucketHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapPost("/bucket/", GetBucketsAsync)
            .WithName("CreateBucket")
            .WithTags("Bucket")
    ];

    /// <summary>Создание нового бакета.</summary>
    private static async Task<Created<BucketDto>> GetBucketsAsync(
        [FromBody] BucketCreateDto createDto,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var newBucket = await clusterConnection.Buckets
            .Value(x => x.BucketName, createDto.BucketName)
            .Value(x => x.NodeId, createDto.NodeId)
            .Value(x => x.Ttl, createDto.TimeToLive)
            .InsertWithOutputAsync(token);
        return TypedResults.Created(
            $"/bucket/{newBucket.BucketId}",
            new BucketDto(
                newBucket.BucketId,
                newBucket.BucketName,
                newBucket.NodeId,
                newBucket.Ttl));
    }
}