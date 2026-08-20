using JetBrains.Annotations;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Api.Handlers.Bucket;
using Storage.Db.Cluster;
using Storage.Node;

namespace Storage.Api.Handlers.File;

[UsedImplicitly]
internal class UploadFileHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapPost("/file/{bucketId}/{**filePath}", UploadFileAsync)
            .DisableAntiforgery()
            .WithName("UploadFile")
            .WithTags("File")
    ];

    /// <summary>Загрузка файла в хранилище.</summary>
    private static async Task<Created<FileDto>> UploadFileAsync(
        [FromRoute] string bucketId,
        [FromRoute] string filePath,
        [FromForm] IFormFile formFile,
        [FromServices] IOptions<StorageOptions> options,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        [FromServices] INodeStorage nodeStorage,
        CancellationToken token)
    {
        filePath = Uri.UnescapeDataString(filePath);
        
        var bucket = await clusterConnection.Buckets.SingleAsync(x => x.BucketId == bucketId, token);
        if (bucket.NodeId != options.Value.NodeId)
            throw new NotImplementedException("Переадресация на другую ноду пока не реализована.");

        using var ms = new MemoryStream();
        await formFile.CopyToAsync(ms, token);

        var bucketStorage = nodeStorage.GetOrCreateBucket(bucket.BucketId);

        var location = bucketStorage.Write(ms.ToArray());

        var file = await clusterConnection.Files
            .Value(x => x.BucketId, bucket.BucketId)
            .Value(x => x.NodeId, bucket.NodeId)
            .Value(x => x.FileName, filePath)
            .Value(x => x.Offset, location.Offset)
            .Value(x => x.PartId, location.PartNumber)
            .InsertWithOutputAsync(token);

        return TypedResults.Created(
            $"/file/{bucketId}/{filePath}",
            new FileDto(
                formFile.FileName,
                bucket.BucketId,
                bucket.NodeId));
    }
}