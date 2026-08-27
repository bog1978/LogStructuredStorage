using Model = Storage.Cluster.DataAccess.Model;

namespace Storage.Api.DataAccess;

public interface IClusterDataAccess
{
    Task<Model.Bucket> CreateBucketAsync(
        string bucketId,
        string nodeId,
        TimeSpan timeToLive,
        CancellationToken token);

    Task<Model.Bucket?> GetBucketAsync(
        string bucketId,
        CancellationToken token);

    Task<IReadOnlyList<Model.Bucket>> GetBucketsAsync(
        CancellationToken token);

    Task<IReadOnlyList<Model.Node>> GetNodesAsync(
        CancellationToken token);

    Task<Model.Bucket?> UpdateBucketAsync(
        string bucketId,
        string? nodeId,
        TimeSpan? timeToLive,
        CancellationToken token);

    Task<Model.File?> GetFileAsync(
        string bucketId,
        string filePath,
        CancellationToken token);

    Task<IReadOnlyList<Model.File>> GetFilesAsync(
        string bucketId,
        int pageNumber,
        int pageSize,
        CancellationToken token);

    Task<Model.File> CreateFileAsync(
        string bucketId,
        string nodeId,
        string filePath,
        long partOffset,
        int partNumber,
        long fileSize,
        CancellationToken token);

    Task RegisterNodeAsync(
        string nodeId,
        string hostName,
        CancellationToken token);
}