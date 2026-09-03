using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Storage.Api.DataAccess;
using Storage.Api.Dto;
using Storage.Api.Exceptions;
using Storage.Api.Handlers.Metadata;
using Storage.Api.Internal;
using Storage.Api.Lss;
using Storage.Api.Options;

namespace Storage.Api.Handlers.File;

[UsedImplicitly]
internal class UploadFileHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapPost("/file/{bucketId}", UploadFileAsync)
            .DisableAntiforgery()
            .WithName("UploadFile")
            .WithTags("File")
    ];

    /// <summary>Загрузка файла в хранилище.</summary>
    private static async Task<Created<string>> UploadFileAsync(
        [FromRoute] string bucketId,
        [FromForm] IFormFile formFile,
        [FromServices] IOptions<StorageOptions> options,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        [FromServices] INodeStorage nodeStorage,
        CancellationToken token)
    {
        var bucket = await clusterDataAccess.GetBucketAsync(bucketId, token);
        if (bucket == null)
            throw new BucketNotFoundException(bucketId);

        if (bucket.NodeId != options.Value.NodeName)
            throw new FeatureNotImplementedException("Переадресация на другую ноду.");

        var bucketStorage = nodeStorage.GetOrCreateBucket(bucket.BucketName);

        var fileHeader = new FileHeader(
            formFile.FileName,
            formFile.ContentType,
            (int)formFile.Length,
            DateTimeOffset.UtcNow);

        await using var data = formFile.OpenReadStream();
        var location = await bucketStorage.Write(fileHeader, data, token);

        var fileKey = MappingExt.GetFileKey(options.Value.NodeName, bucketId, location.PartNumber, location.Offset);

        logger.LogInformation("File uploaded. Key: {key}", fileKey);

        return TypedResults.Created($"/file/{fileKey}", fileKey);
    }
}