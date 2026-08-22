using Storage.Cluster.Model;
using File = Storage.Cluster.Model.File;

namespace Storage.Cluster.DataAccess;

public interface IClusterDataAccess
{
    Task<Bucket> CreateBucketAsync(
        string bucketId,
        string nodeId,
        TimeSpan timeToLive,
        CancellationToken token);
    
    Task<Bucket?> GetBucketAsync(
        string bucketId,
        CancellationToken token);
    
    Task<IReadOnlyList<Bucket>> GetBucketsAsync(
        CancellationToken token);
    
    Task<IReadOnlyList<Node>> GetNodesAsync(
        CancellationToken token);

    Task<Bucket?> UpdateBucketAsync(
        string bucketId,
        string? nodeId,
        TimeSpan? timeToLive,
        CancellationToken token);

    Task<File?> GetFileAsync(
        string bucketId,
        string filePath,
        CancellationToken token);

    Task<IReadOnlyList<File>> GetFilesAsync(
        string bucketId,
        int pageNumber,
        int pageSize,
        CancellationToken token);

    Task<File> CreateFileAsync(
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