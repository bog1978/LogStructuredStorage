using Model = Storage.Cluster.DataAccess.Model;

namespace Storage.Api.DataAccess;

internal interface IClusterDataAccess
{
    Task<Model.Bucket> CreateBucketAsync(
        string bucketName,
        string nodeId,
        TimeSpan ttlHot,
        TimeSpan ttlCold,
        CancellationToken token);

    Task<Model.Bucket?> GetBucketAsync(
        string bucketName,
        CancellationToken token);

    Task<IReadOnlyList<Model.Bucket>> GetBucketsAsync(
        CancellationToken token);

    Task<IReadOnlyList<Model.Node>> GetNodesAsync(
        CancellationToken token);

    Task<Model.Bucket?> UpdateBucketAsync(
        string bucketName,
        string? nodeId,
        TimeSpan? ttlHot,
        TimeSpan? ttlCold,
        CancellationToken token);

    Task<Model.Node> RegisterNodeAsync(
        string nodeName,
        string hostName,
        CancellationToken token);
}