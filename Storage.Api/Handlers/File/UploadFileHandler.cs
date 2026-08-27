using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Api.Exceptions;
using Storage.Api.Handlers.Metadata;
using Storage.Cluster;
using Storage.Cluster.DataAccess;

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
        [FromServices] IOptions<ClusterOptions> options,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        [FromServices] INodeStorage nodeStorage,
        CancellationToken token)
    {
        filePath = Uri.UnescapeDataString(filePath);

        var bucket = await clusterDataAccess.GetBucketAsync(bucketId, token);
        if (bucket == null)
            throw new BucketNotFoundException(bucketId);

        if (bucket.NodeId != options.Value.NodeId)
            throw new FeatureNotImplementedException("Переадресация на другую ноду.");

        using var ms = new MemoryStream();
        await formFile.CopyToAsync(ms, token);

        var bucketStorage = nodeStorage.GetOrCreateBucket(bucket.BucketId);

        var location = bucketStorage.Write(ms.ToArray());

        var file = await clusterDataAccess.CreateFileAsync(
            bucket.BucketId,
            bucket.NodeId,
            filePath,
            location.Offset,
            location.PartNumber,
            formFile.Length,
            token);

        return TypedResults.Created(
            $"/file/{bucketId}/{filePath.TrimStart('/')}",
            file.ToDto());
    }
}