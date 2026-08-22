using JetBrains.Annotations;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Api.Handlers.Metadata;
using Storage.Db.Cluster;

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
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var files = await clusterConnection.Files
            .Where(x => x.BucketId == bucketId)
            .OrderBy(x => x.FileId)
            .Skip(pageSize ?? 100 * pageNumber ?? 0)
            .Take(pageSize ?? 100)
            .Select(x => x.ToDto())
            .ToListAsync(token);
        
        return TypedResults.Ok(files);
    }
}