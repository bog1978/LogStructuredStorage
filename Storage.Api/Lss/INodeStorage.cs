namespace Storage.Api.Lss;

internal interface INodeStorage : IDisposable
{
    IBucketStorage GetBucket(string bucketName);
    IBucketStorage GetOrCreateBucket(string bucketName);
    void DeleteAll();
    IReadOnlyDictionary<string, List<int>> ApplyRetentionPolicy(Func<string, RetentionPolicy> policyFunc);
}