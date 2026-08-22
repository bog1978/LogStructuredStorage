using Microsoft.Extensions.Options;
using Storage.Cluster.DataAccess;

namespace Storage.Cluster.Services;

internal sealed class NodeInitializer(
    IClusterDataAccess clusterDataAccess,
    IOptions<ClusterOptions> options)
{
    private readonly ClusterOptions _options = options.Value;

    public async Task InitializeAsync(CancellationToken token) => 
        await clusterDataAccess.RegisterNodeAsync(_options.NodeId, "localhost", token);
}