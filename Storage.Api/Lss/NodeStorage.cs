using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Storage.Api.Options;
using Storage.Cluster;

namespace Storage.Api.Lss;

internal sealed class NodeStorage : INodeStorage
{
    private readonly StorageOptions _options;

    private readonly ConcurrentDictionary<string, BucketStorage> _bucketMap = new();

    public NodeStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        LoadBuckets();
    }

    public IBucketStorage GetBucket(string bucketName) =>
        _bucketMap.TryGetValue(bucketName, out var bucketStorage)
            ? bucketStorage
            : throw new InvalidOperationException("Bucket not found");

    public IBucketStorage GetOrCreateBucket(string bucketName) =>
        _bucketMap.GetOrAdd(bucketName, key => new(
            _options.HotPath,
            _options.ColdPath,
            key,
            _options.PartSizeMb));

    public async Task DeleteAll(CancellationToken token)
    {
        foreach (var bucketStorage in _bucketMap.Values)
            await bucketStorage.DeleteAll(token);
        _bucketMap.Clear();
    }

    public async Task ApplyRetentionPolicy(Func<string, RetentionPolicy> policyFunc, CancellationToken token)
    {
        foreach (var bucketStorage in _bucketMap.Values)
            await bucketStorage.ApplyRetentionPolicy(policyFunc(bucketStorage.Name), token);
    }

    public void Dispose()
    {
        foreach (var bucketStorage in _bucketMap.Values)
            bucketStorage.Dispose();
    }

    private void LoadBuckets()
    {
        if (!Directory.Exists(_options.HotPath))
            Directory.CreateDirectory(_options.HotPath);
        var bucketDirs = Directory.EnumerateDirectories(_options.HotPath);
        foreach (var bucketDir in bucketDirs)
        {
            var bucketName = Path.GetFileName(bucketDir);
            var bucketStorage = new BucketStorage(
                _options.HotPath,
                _options.ColdPath,
                bucketName,
                _options.PartSizeMb);
            if (!_bucketMap.TryAdd(bucketName, bucketStorage))
                throw new InvalidOperationException($"Bucket {bucketName} already exists");
        }
    }
}