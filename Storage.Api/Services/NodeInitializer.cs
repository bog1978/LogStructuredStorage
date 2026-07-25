using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Options;
using Storage.Db.Cluster;

namespace Storage.Api.Services;

internal class NodeInitializer(ClusterConnection clusterConnection, IOptions<StorageOptions> options)
{
    private readonly StorageOptions _options = options.Value;

    public async Task InitializeAsync(CancellationToken token)
    {
        var node = await clusterConnection.Nodes
            .Where(x => x.NodeName == _options.NodeName)
            .SingleOrDefaultAsync(token);

        if (node == null)
            await clusterConnection.Nodes
                .Value(x => x.NodeName, _options.NodeName)
                .Value(x => x.HostName, "localhost")
                .InsertAsync(token);
    }
}