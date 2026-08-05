namespace Storage.Api.Services;

internal sealed class BucketStorage : IDisposable
{
    private readonly int _partSize;
    private readonly string _bucketDir;
    private PartStorage _partStorage;

    public BucketStorage(string rootDir, string bucketName, int partSize)
    {
        _partSize = partSize;
        _bucketDir = Path.Combine(rootDir, bucketName);
        if (!Directory.Exists(_bucketDir))
            Directory.CreateDirectory(_bucketDir);
        var lastFile = Directory
            .EnumerateFiles(_bucketDir, "*.lss", SearchOption.AllDirectories)
            .OrderBy(Path.GetFileNameWithoutExtension)
            .LastOrDefault();
        _partStorage = lastFile != null
            ? new PartStorage(lastFile)
            : new PartStorage(_bucketDir, 1, partSize);
    }

    public long Write(byte[] data)
    {
        if (_partStorage.TryWrite(data, out var offset))
            return offset;

        var nextPartNumber = _partStorage.PartNumber + 1;
        _partStorage.Dispose();
        _partStorage = new PartStorage(_bucketDir, nextPartNumber, _partSize);
        return !_partStorage.TryWrite(data, out offset)
            ? throw new InvalidOperationException("Failed to write data")
            : offset;
    }

    public byte[] Read(long offset)
    {
        return _partStorage.Read(offset);
    }

    public void Dispose()
    {
        _partStorage.Dispose();
    }
}