using System.Globalization;
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
using Storage.Cluster;
using Storage.Cluster.DataAccess;

namespace Storage.Api.Handlers.File;

[UsedImplicitly]
internal class DownloadFileHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/file/{fileKey}", DownloadFileAsync)
            .DisableAntiforgery()
            .WithName("DownloadFile")
            .WithTags("File")
    ];

    /// <summary>Скачивание файла из хранилища.</summary>
    /// <param name="fileKey">Ключ файла.</param>
    private static async Task<FileStreamHttpResult> DownloadFileAsync(
        [FromRoute] string fileKey,
        [FromServices] IOptions<StorageOptions> options,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        [FromServices] INodeStorage nodeStorage,
        CancellationToken token)
    {
        var keyParts = fileKey.Split(':');
        var nodeName = keyParts[0];
        var bucketName = keyParts[1];
        var partNumber = int.Parse(keyParts[2], CultureInfo.InvariantCulture);
        var partOffset = long.Parse(keyParts[3], CultureInfo.InvariantCulture);

        if (nodeName != options.Value.NodeName)
            throw new FeatureNotImplementedException("Переадресация на другую ноду.");

        var location = new DataLocation(bucketName, partNumber, partOffset);

        var bucketStorage = nodeStorage.GetOrCreateBucket(bucketName);
        var (fileName, data, createdAt) = bucketStorage.Read(location);
        var ms = new MemoryStream(data);

        return TypedResults.Stream(ms, fileDownloadName: fileName, lastModified: createdAt);
    }
}