using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Options;
using Storage.Db.Cluster;

namespace Storage.Api.Services;

internal sealed class NodeInitializer(ClusterConnection clusterConnection, IOptions<ApiOptions> options)
{
    private readonly ApiOptions _options = options.Value;

    public async Task InitializeAsync(CancellationToken token)
    {
        var node = await clusterConnection.Nodes
            .Where(x => x.NodeId == _options.NodeId)
            .SingleOrDefaultAsync(token);

        if (node == null)
            await clusterConnection.Nodes
                .Value(x => x.NodeId, _options.NodeId)
                .Value(x => x.HostName, "localhost")
                .InsertAsync(token);
    }
}