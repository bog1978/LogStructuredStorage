using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Storage.Api.Dto;
using Storage.Api.Internal;
using Storage.Cluster;
using Storage.Cluster.DataAccess;

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
        [FromServices] IClusterDataAccess clusterDataAccess,
        CancellationToken token)
    {
        var nodes = await clusterDataAccess.GetNodesAsync(token);
        return TypedResults.Ok(nodes
            .Select(x => x.ToDto())
            .ToList());
    }
}