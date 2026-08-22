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
using Storage.Node;

namespace Storage.Api.Handlers.File;

[UsedImplicitly]
internal class DownloadFileHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/file/{bucketId}/{**filePath}", DownloadFileAsync)
            .DisableAntiforgery()
            .WithName("DownloadFile")
            .WithTags("File")
    ];

    /// <summary>Скачивание файла из хранилища.</summary>
    /// <param name="bucketId">Идентификатор корзины.</param>
    /// <param name="filePath">Путь к файлу в корзине.</param>
    private static async Task<FileStreamHttpResult> DownloadFileAsync(
        [FromRoute] string bucketId,
        [FromRoute] string filePath,
        [FromServices] IOptions<ClusterOptions> options,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        [FromServices] INodeStorage nodeStorage,
        CancellationToken token)
    {
        filePath = Uri.UnescapeDataString(filePath);
        var fileName = Path.GetFileName(filePath);

        var file = await clusterDataAccess.GetFileAsync(bucketId, filePath, token);
        if (file == null)
            throw new BucketFileNotFoundException(bucketId, filePath);

        if (file.NodeId != options.Value.NodeId)
            throw new FeatureNotImplementedException("Переадресация на другую ноду.");

        var bucketStorage = nodeStorage.GetOrCreateBucket(file.BucketId);
        var data = bucketStorage.Read(file.Location);
        var ms = new MemoryStream(data);

        return TypedResults.Stream(ms, fileDownloadName: fileName);
    }
}