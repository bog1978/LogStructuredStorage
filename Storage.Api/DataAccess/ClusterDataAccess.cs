using LinqToDB;
using LinqToDB.Async;
using Model = Storage.Cluster.DataAccess.Model;

namespace Storage.Api.DataAccess;

internal class ClusterDataAccess(Model.ClusterConnection clusterConnection) : IClusterDataAccess
{
    public async Task<Model.Bucket> CreateBucketAsync(
        string bucketName,
        string nodeId,
        TimeSpan ttlHot,
        TimeSpan ttlCold,
        CancellationToken token) =>
        await clusterConnection.Buckets
            .Where(x => x.BucketName == bucketName)
            .SingleOrDefaultAsync(token) ??
        await clusterConnection.Buckets
            .Value(x => x.BucketName, bucketName)
            .Value(x => x.NodeId, nodeId)
            .Value(x => x.TtlHot, ttlHot)
            .Value(x => x.TtlCold, ttlCold)
            .InsertWithOutputAsync(token);

    public async Task<Model.Bucket?> GetBucketAsync(
        string bucketName,
        CancellationToken token) =>
        await clusterConnection.Buckets
            .Where(x => x.BucketName == bucketName)
            .SingleOrDefaultAsync(token);

    public async Task<IReadOnlyList<Model.Bucket>> GetBucketsAsync(
        CancellationToken token) =>
        await clusterConnection.Buckets
            .ToListAsync(token);

    public async Task<IReadOnlyList<Model.Node>> GetNodesAsync(
        CancellationToken token) =>
        await clusterConnection.Nodes
            .ToListAsync(token);

    public async Task<Model.Bucket?> UpdateBucketAsync(
        string bucketName,
        string? nodeId,
        TimeSpan? ttlHot,
        TimeSpan? ttlCold,
        CancellationToken token)
    {
        var updatedList = await clusterConnection.Buckets
            .Where(x => x.BucketName == bucketName)
            .AsUpdatable()
            .SetIf(nodeId != null, x => x.NodeId, () => nodeId)
            .SetIf(ttlHot != null, x => x.TtlHot, () => ttlHot)
            .SetIf(ttlCold != null, x => x.TtlCold, () => ttlCold)
            .UpdateWithOutputAsync((del, ins) => ins)
            .ToListAsync(token);
        return updatedList.SingleOrDefault();
    }

    public async Task<Model.Node> RegisterNodeAsync(
        string nodeName,
        string hostName,
        CancellationToken token) =>
        await clusterConnection.Nodes
            .Where(x => x.NodeName == nodeName)
            .SingleOrDefaultAsync(token) ??
        await clusterConnection.Nodes
            .Value(x => x.NodeName, nodeName)
            .Value(x => x.HostName, hostName)
            .InsertWithOutputAsync(token);
}