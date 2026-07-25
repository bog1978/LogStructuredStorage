using JetBrains.Annotations;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers;

[UsedImplicitly]
internal sealed class GetBucketsHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/buckets/", GetBucketsAsync)
            .WithName("GetBuckets")
            .WithTags("Buckets")
    ];

    /// <summary>Список бакетов.</summary>
    private static async Task<Ok<List<Bucket>>> GetBucketsAsync(
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var buckets = await clusterConnection.Buckets.ToListAsync(token);
        return TypedResults.Ok(buckets);
    }
}