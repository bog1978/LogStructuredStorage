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
using Storage.Api.Lss.Model;
using Storage.Api.Options;
using Storage.Api.Validation;

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
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Загрузка файла {formFile.FileName} в корзину {bucketId}");

        var normalizedBucketName = BucketNameValidator.NormalizeAndValidate(bucketId);
        var bucket = await clusterDataAccess.GetBucketAsync(normalizedBucketName, token);
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

        var fileKey = MappingExt.GetFileKey(options.Value.NodeName, bucket.BucketName, location.PartNumber, location.Offset);

        activity?.AddEvent($"Файл {formFile.FileName} успешно загружен. Ключ: {fileKey}");

        return TypedResults.Created($"/file/{fileKey}", fileKey);
    }
}
