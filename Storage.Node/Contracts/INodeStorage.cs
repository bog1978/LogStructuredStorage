namespace Storage.Node;

public interface INodeStorage : IDisposable
{
    IBucketStorage GetBucket(string bucketName);
    IBucketStorage GetOrCreateBucket(string bucketName);
    void DeleteAll();
    void ApplyRetentionPolicy(RetentionPolicy policy);
}