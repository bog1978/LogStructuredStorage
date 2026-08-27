using LinqToDB;
using LinqToDB.Async;

namespace Storage.Api.DataAccess;

internal class ClusterDataAccess(Cluster.DataAccess.Model.ClusterConnection clusterConnection) : IClusterDataAccess
{
    public async Task<Cluster.DataAccess.Model.Bucket> CreateBucketAsync(
        string bucketId,
        string nodeId,
        TimeSpan timeToLive,
        CancellationToken token) =>
        await clusterConnection.Buckets
            .Where(x => x.BucketId == bucketId)
            .SingleOrDefaultAsync(token) ??
        await clusterConnection.Buckets
            .Value(x => x.BucketId, bucketId)
            .Value(x => x.NodeId, nodeId)
            .Value(x => x.Ttl, timeToLive)
            .InsertWithOutputAsync(token);

    public async Task<Cluster.DataAccess.Model.Bucket?> GetBucketAsync(
        string bucketId,
        CancellationToken token) =>
        await clusterConnection.Buckets
            .Where(x => x.BucketId == bucketId)
            .SingleOrDefaultAsync(token);

    public async Task<IReadOnlyList<Cluster.DataAccess.Model.Bucket>> GetBucketsAsync(
        CancellationToken token) =>
        await clusterConnection.Buckets
            .ToListAsync(token);

    public async Task<IReadOnlyList<Cluster.DataAccess.Model.Node>> GetNodesAsync(
        CancellationToken token) =>
        await clusterConnection.Nodes
            .ToListAsync(token);

    public async Task<Cluster.DataAccess.Model.Bucket?> UpdateBucketAsync(
        string bucketId,
        string? nodeId,
        TimeSpan? timeToLive,
        CancellationToken token)
    {
        var updatedList = await clusterConnection.Buckets
            .Where(x => x.BucketId == bucketId)
            .AsUpdatable()
            .SetIf(nodeId != null, x => x.NodeId, () => nodeId)
            .SetIf(timeToLive != null, x => x.Ttl, () => timeToLive)
            .UpdateWithOutputAsync((del, ins) => ins)
            .ToListAsync(token);
        return updatedList.SingleOrDefault();
    }

    public async Task<Cluster.DataAccess.Model.File?> GetFileAsync(
        string bucketId,
        string filePath,
        CancellationToken token) =>
        await clusterConnection.Files
            .Where(x => x.BucketId == bucketId && x.FileName == filePath)
            .SingleOrDefaultAsync(token);

    public async Task<IReadOnlyList<Cluster.DataAccess.Model.File>> GetFilesAsync(
        string bucketId,
        int pageNumber,
        int pageSize,
        CancellationToken token) =>
        await clusterConnection.Files
            .Where(x => x.BucketId == bucketId)
            .OrderBy(x => x.FileId)
            .Skip(pageSize * pageNumber)
            .Take(pageSize)
            .ToListAsync(token);

    public async Task<Cluster.DataAccess.Model.File> CreateFileAsync(
        string bucketId,
        string nodeId,
        string filePath,
        long partOffset,
        int partNumber,
        long fileSize,
        CancellationToken token)
    {
        var existing = await GetFileAsync(bucketId, filePath, token);
        if (existing == null)
            return await clusterConnection.Files
                .Value(x => x.BucketId, bucketId)
                .Value(x => x.FileName, filePath)
                .Value(x => x.NodeId, nodeId)
                .Value(x => x.PartOffset, partOffset)
                .Value(x => x.PartId, partNumber)
                .Value(x => x.FileSize, fileSize)
                .InsertWithOutputAsync(token);
        return await clusterConnection.Files
            .Where(x => x.BucketId == bucketId && x.FileName == filePath)
            .Set(x => x.NodeId, nodeId)
            .Set(x => x.PartOffset, partOffset)
            .Set(x => x.PartId, partNumber)
            .Set(x => x.FileSize, fileSize)
            .UpdateWithOutputAsync((del, ins) => ins)
            .SingleAsync(token);
    }

    public async Task RegisterNodeAsync(
        string nodeId,
        string hostName,
        CancellationToken token)
    {
        var node = await clusterConnection.Nodes
            .Where(x => x.NodeId == nodeId)
            .SingleOrDefaultAsync(token);

        if (node == null)
            await clusterConnection.Nodes
                .Value(x => x.NodeId, nodeId)
                .Value(x => x.HostName, hostName)
                .InsertAsync(token);
    }

    public async Task DeleteFilesAsync(
        string nodeId,
        string bucketId,
        int partNumber) =>
        await clusterConnection.Files
            .Where(x => x.NodeId == nodeId && x.BucketId == bucketId && x.PartId == partNumber)
            .DeleteAsync();
}