namespace Storage.Node.Impl;

internal sealed class BucketStorage : IBucketStorage
{
    private readonly string _bucketName;
    private readonly int _partSize;
    private readonly string _bucketDir;
    private readonly Dictionary<int, PartStorage> _parts = new();
    private PartStorage _partStorage;

    public BucketStorage(string rootDir, string bucketName, int partSize)
    {
        _bucketName = bucketName;
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

        if (_partStorage == null)
        {
            var nextPartNumber = _parts.Keys.Count > 0
                ? _parts.Keys.Max() + 1
                : 0;
            _partStorage = new PartStorage(_bucketDir, nextPartNumber, partSize);
            _parts.Add(nextPartNumber, _partStorage);
        }
    }

    public DataLocation Write(byte[] data)
    {
        if (_partStorage.TryWrite(data, out var offset))
            return new(_bucketName, _partStorage.PartNumber, offset);

        _partStorage.Close();
        var nextPartNumber = _partStorage.PartNumber + 1;
        _partStorage = new PartStorage(_bucketDir, nextPartNumber, _partSize);
        _parts.Add(nextPartNumber, _partStorage);
        return !_partStorage.TryWrite(data, out offset)
            ? throw new InvalidOperationException("Failed to write data")
            : new(_bucketName, _partStorage.PartNumber, offset);
    }

    public byte[] Read(DataLocation location) =>
        _parts.TryGetValue(location.PartNumber, out var part)
            ? part.Read(location.Offset)
            : throw new InvalidOperationException($"Part {location.PartNumber} not found");

    public void DeleteAll()
    {
        foreach (var part in _parts.Values)
            part.DeleteAll();
        _parts.Clear();
        if (Directory.Exists(_bucketDir))
            Directory.Delete(_bucketDir, true);
    }

    public void Dispose()
    {
        foreach (var part in _parts.Values)
            part.Dispose();
    }
}