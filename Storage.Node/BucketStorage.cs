namespace Storage.Node;

internal sealed class BucketStorage : IDisposable
{
    private readonly int _partSize;
    private readonly string _bucketDir;
    private readonly Dictionary<int, PartStorage> _parts = new();
    private PartStorage _partStorage;

    public BucketStorage(string rootDir, string bucketName, int partSize)
    {
        _partSize = partSize;
        _bucketDir = Path.Combine(rootDir, bucketName);
        if (!Directory.Exists(_bucketDir))
            Directory.CreateDirectory(_bucketDir);
        var partFiles = Directory
            .EnumerateFiles(_bucketDir, "*.lss", SearchOption.AllDirectories);
        foreach (var partFile in partFiles)
        {
            var part = new PartStorage(partFile);
            _parts.Add(part.PartNumber, part);
            if (part.CanWrite)
            {
                if (_partStorage != null)
                    throw new InvalidOperationException($"Part {part.PartNumber} is already written");
                _partStorage = part;
            }
        }

        _partStorage ??= new PartStorage(_bucketDir, 0, partSize);
    }

    public DataLocation Write(byte[] data)
    {
        if (_partStorage.TryWrite(data, out var offset))
            return new(_partStorage.PartNumber, offset);

        _partStorage.Close();
        _parts.Add(_partStorage.PartNumber, _partStorage);
        var nextPartNumber = _partStorage.PartNumber + 1;
        _partStorage = new PartStorage(_bucketDir, nextPartNumber, _partSize);
        return !_partStorage.TryWrite(data, out offset)
            ? throw new InvalidOperationException("Failed to write data")
            : new(_partStorage.PartNumber, offset);
    }

    public byte[] Read(DataLocation location) =>
        _parts.TryGetValue(location.PartNumber, out var part)
            ? part.Read(location.Offset)
            : throw new InvalidOperationException($"Part {location.PartNumber} not found");

    public void Dispose()
    {
        foreach (var (k, v) in _parts)
            v.Dispose();
    }
}