using JetBrains.Annotations;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Hosting;
using Storage.Api.Dto;
using Storage.Db.Cluster;

namespace Storage.Api.Handlers.Metadata;

[UsedImplicitly]
internal sealed class GetNodesHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapGet("/node/", GetNodesAsync)
            .WithName("GetNodes")
            .WithTags("Metadata")
    ];

    /// <summary>Список узлов в кластере.</summary>
    private static async Task<Ok<List<NodeDto>>> GetNodesAsync(
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] ClusterConnection clusterConnection,
        CancellationToken token)
    {
        var nodes = await clusterConnection.Nodes
            .Select(x => x.ToDto())
            .ToListAsync(token);
        return TypedResults.Ok(nodes);
    }
}