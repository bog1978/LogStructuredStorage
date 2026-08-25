using System.Collections.Concurrent;

namespace Storage.Node.Impl;

internal sealed class BucketStorage : IBucketStorage
{
    private readonly string _bucketName;
    private readonly int _partSize;
    private readonly string _bucketDir;
    private readonly ConcurrentDictionary<int, PartStorage> _partsMap = new();
    private PartStorage _partStorage;

    public BucketStorage(string rootDir, string bucketName, int partSize)
    {
        _bucketName = bucketName;
        _partSize = partSize;
        _bucketDir = Path.Combine(rootDir, bucketName);
        if (!Directory.Exists(_bucketDir))
            Directory.CreateDirectory(_bucketDir);

        LoadParts();
        _partStorage ??= AddActivePart();
    }

    public DataLocation Write(byte[] data)
    {
        if (_partStorage.TryWrite(data, out var offset))
            return new(_bucketName, _partStorage.PartNumber, offset);

        _partStorage.Close();
        _partStorage = AddActivePart();

        return !_partStorage.TryWrite(data, out offset)
            ? throw new InvalidOperationException("Failed to write data")
            : new(_bucketName, _partStorage.PartNumber, offset);
    }
    
    public string Name => _bucketName;

    public byte[] Read(DataLocation location) =>
        _partsMap.TryGetValue(location.PartNumber, out var part)
            ? part.Read(location.Offset)
            : throw new InvalidOperationException($"Part {location.PartNumber} not found");

    public void DeleteAll()
    {
        foreach (var part in _partsMap.Values)
            part.Delete();
        _partsMap.Clear();
        if (Directory.Exists(_bucketDir))
            Directory.Delete(_bucketDir, true);
    }

    public void Dispose()
    {
        foreach (var part in _partsMap.Values)
            part.Dispose();
    }

    public void ApplyRetentionPolicy(RetentionPolicy policy)
    {
        foreach (var part in _partsMap.Values)
            if (part.MaxTime < DateTimeOffset.Now + policy.Ttl)
                part.Delete();
    }

    private void LoadParts()
    {
        var partFiles = Directory
            .EnumerateFiles(_bucketDir, "*.lss", SearchOption.AllDirectories);
        foreach (var partFile in partFiles)
        {
            var part = new PartStorage(partFile);
            if (!_partsMap.TryAdd(part.PartNumber, part))
                throw new InvalidOperationException($"Duplicate part number {part.PartNumber}");
            if (!part.CanWrite)
                continue;
            if (_partStorage != null)
                throw new InvalidOperationException(
                    $"Active part already exists: {_partStorage.PartNumber}. Duplicate active part: {part.PartNumber}");
            _partStorage = part;
        }
    }

    private PartStorage AddActivePart()
    {
        var nextPartNumber = _partsMap.Keys.Count > 0
            ? _partsMap.Keys.Max() + 1
            : 0;
        var partStorage = new PartStorage(_bucketDir, nextPartNumber, _partSize);
        if (!_partsMap.TryAdd(nextPartNumber, partStorage))
            throw new InvalidOperationException($"Duplicate part number {nextPartNumber}");
        return partStorage;
    }
}