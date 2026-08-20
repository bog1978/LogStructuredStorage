using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Storage.Node;

internal sealed class NodeStorage : INodeStorage
{
    private readonly NodeStorageOptions _options;

    private readonly ConcurrentDictionary<string, BucketStorage> _bucketMap = new();

    public NodeStorage(IOptions<NodeStorageOptions> options)
    {
        _options = options.Value;
        LoadBuckets();
    }

    public IBucketStorage GetBucket(string bucketName) =>
        _bucketMap.TryGetValue(bucketName, out var bucketStorage)
            ? bucketStorage
            : throw new InvalidOperationException("Bucket not found");

    public IBucketStorage GetOrCreateBucket(string bucketName) =>
        _bucketMap.GetOrAdd(bucketName, _ => new BucketStorage(_options.RootPath, bucketName, _options.PartSize));

    public void DeleteAll()
    {
        foreach (var bucketStorage in _bucketMap.Values)
            bucketStorage.DeleteAll();
        _bucketMap.Clear();
    }

    public void Dispose()
    {
        Close();
    }

    private void LoadBuckets()
    {
        if (!Directory.Exists(_options.RootPath))
            Directory.CreateDirectory(_options.RootPath);
        var bucketDirs = Directory.EnumerateDirectories(_options.RootPath);
        foreach (var bucketDir in bucketDirs)
        {
            var bucketName = Path.GetFileName(bucketDir);
            var bucketStorage = new BucketStorage(_options.RootPath, bucketName, _options.PartSize);
            if (!_bucketMap.TryAdd(bucketName, bucketStorage))
                throw new InvalidOperationException($"Bucket {bucketName} already exists");
        }
    }
    
    private void Close()
    {
        foreach (var bucketStorage in _bucketMap.Values)
            bucketStorage.Dispose();
    }
}