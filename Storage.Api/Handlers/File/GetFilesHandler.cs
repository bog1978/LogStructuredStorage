using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Storage.Api.DataAccess;
using Storage.Api.Dto;
using Storage.Api.Handlers.Metadata;
using Storage.Api.Internal;
using Storage.Cluster;
using Storage.Cluster.DataAccess;

namespace Storage.Api.Handlers.File;

[UsedImplicitly]
internal sealed class GetFilesHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/file/{bucketId}", GetFilesAsync)
            .WithName("GetFiles")
            .WithTags("File")
    ];

    /// <summary>Список файлов корзины.</summary>
    /// <param name="bucketId">Идентификатор корзины.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 0).</param>
    /// <param name="pageSize">Размер страницы.</param>
    private static async Task<Ok<List<FileDto>>> GetFilesAsync(
        [FromRoute] string bucketId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        CancellationToken token)
    {
        var files = await clusterDataAccess.GetFilesAsync(
            bucketId,
            pageNumber ?? 0,
            pageSize ?? 100,
            token);
        return TypedResults.Ok(files
            .Select(x => x.ToDto())
            .ToList());
    }
}