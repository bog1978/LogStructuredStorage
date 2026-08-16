namespace Storage.Node;

public sealed class NodeStorage : IDisposable
{
    private readonly string _rootDir;
    private readonly int _partSize;
    private readonly Dictionary<string, BucketStorage> _bucketMap = new();

    public NodeStorage(string rootDir, int partSize)
    {
        _rootDir = rootDir;
        _partSize = partSize;
        LoadBuckets();
    }

    public byte[] Read(DataLocation location)
    {
        if (!_bucketMap.TryGetValue(location.BucketName, out var bucketStorage))
            throw new InvalidOperationException("Bucket not found");
        return bucketStorage.Read(location);
    }

    public DataLocation Write(string bucketName, byte[] data)
    {
        if (!_bucketMap.TryGetValue(bucketName, out var bucketStorage))
        {
            bucketStorage = new BucketStorage(_rootDir, bucketName, _partSize);
            _bucketMap.Add(bucketName, bucketStorage);
        }

        return bucketStorage.Write(data);
    }
    
    public void Dispose()
    {
        foreach (var bucketStorage in _bucketMap.Values)
            bucketStorage.Dispose();
    }

    private void LoadBuckets()
    {
        var bucketDirs = Directory.EnumerateDirectories(_rootDir);
        foreach (var bucketDir in bucketDirs)
        {
            var bucketName = Path.GetFileName(bucketDir);
            var bucketStorage = new BucketStorage(_rootDir, bucketName, _partSize);
            _bucketMap.Add(bucketName, bucketStorage);
        }
    }
}