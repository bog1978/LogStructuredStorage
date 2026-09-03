using System.Globalization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Storage.Api.DataAccess;
using Storage.Api.Exceptions;
using Storage.Api.Handlers.Metadata;
using Storage.Api.Internal;
using Storage.Api.Lss;
using Storage.Api.Options;
using System.Net.Http.Headers;

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
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, contentType: "application/octet-stream")]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    private static async Task DownloadFileAsync(
        [FromRoute] string fileKey,
        [FromServices] IOptions<StorageOptions> options,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        [FromServices] INodeStorage nodeStorage,
        HttpContext context,
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
        await bucketStorage.Read(location, SetupHeaders, context.Response.Body, token);

        return;
        
        void SetupHeaders(FileHeader fileHeader)
        {
            var contentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = fileHeader.FileName,
                FileNameStar = fileHeader.FileName,
            };
            
            if (fileHeader.Length > 0)
            {
                contentDisposition.Size = fileHeader.Length;
                context.Response.ContentLength = fileHeader.Length;
            }
            
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = string.IsNullOrWhiteSpace(fileHeader.ContentType)
                ? "application/octet-stream"
                : fileHeader.ContentType;
            context.Response.Headers.ContentDisposition = contentDisposition.ToString();
        }
    }
}