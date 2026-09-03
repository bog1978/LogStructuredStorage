namespace Storage.Api.Lss;

internal interface INodeStorage : IDisposable
{
    IBucketStorage GetBucket(string bucketName);
    IBucketStorage GetOrCreateBucket(string bucketName);
    Task DeleteAll(CancellationToken token);
    Task ApplyRetentionPolicy(Func<string, RetentionPolicy> policyFunc, CancellationToken token);
}