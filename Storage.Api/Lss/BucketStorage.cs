using System.Collections.Concurrent;

namespace Storage.Api.Lss;

internal sealed class BucketStorage : IBucketStorage
{
    private readonly string _bucketName;
    private readonly int _partSizeMb;
    private readonly string _bucketHotDir;
    private readonly string _bucketColdDir;
    private readonly ConcurrentDictionary<int, PartStorage> _partsMap = new();
    private PartStorage _partStorage;

    public BucketStorage(string hotDir, string coldDir, string bucketName, int partSizeMb)
    {
        _bucketName = bucketName;
        _partSizeMb = partSizeMb;
        _bucketHotDir = Path.Combine(hotDir, bucketName);
        _bucketColdDir = Path.Combine(coldDir, bucketName);
        LoadParts(_bucketHotDir, true);
        LoadParts(_bucketColdDir, false);
        _partStorage ??= AddActivePart();
    }

    public DataLocation Write(string fileName, byte[] data)
    {
        if (_partStorage.TryWrite(fileName, data, out var offset))
            return new(_bucketName, _partStorage.PartNumber, offset);

        _partStorage.Close();
        _partStorage = AddActivePart();

        return !_partStorage.TryWrite(fileName, data, out offset)
            ? throw new InvalidOperationException("Failed to write data")
            : new(_bucketName, _partStorage.PartNumber, offset);
    }

    public string Name => _bucketName;

    public (string fileName, byte[] data, DateTimeOffset createdAt) Read(DataLocation location) =>
        _partsMap.TryGetValue(location.PartNumber, out var part)
            ? part.Read(location.Offset)
            : throw new InvalidOperationException($"Part {location.PartNumber} not found");

    public void DeleteAll()
    {
        foreach (var part in _partsMap.Values)
            part.Delete();
        _partsMap.Clear();
        if (Directory.Exists(_bucketHotDir))
            Directory.Delete(_bucketHotDir, true);
    }

    public void Dispose()
    {
        foreach (var part in _partsMap.Values)
            part.Dispose();
    }

    public IReadOnlyList<int> ApplyRetentionPolicy(RetentionPolicy policy)
    {
        var removed = new List<int>();
        var parts = _partsMap.Values.ToList();
        foreach (var part in parts)
        {
            if (part.CanWrite)
                continue;
            // Полное время жизни складывается из горячего и холодного.
            if (part.MaxTime + policy.TtlHot + policy.TtlCold < DateTimeOffset.Now)
            {
                part.Delete();
                if (_partsMap.Remove(part.PartNumber, out var p))
                    removed.Add(p.PartNumber);
            }
            else if (part.IsHot && part.MaxTime + policy.TtlHot < DateTimeOffset.Now)
            {
                part.MakeCold(_bucketColdDir);
            }
            else
            {
                // Пускай еще побудет тепленьким.
            }
        }

        return removed.AsReadOnly();
    }

    private void LoadParts(string bucketDir, bool isHot)
    {
        if (!Directory.Exists(bucketDir))
        {
            Directory.CreateDirectory(bucketDir);
            return;
        }

        var partFiles = Directory
            .EnumerateFiles(bucketDir, "*.lss", SearchOption.AllDirectories);
        foreach (var partFile in partFiles)
        {
            var part = new PartStorage(partFile, isHot);
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
        var partStorage = new PartStorage(_bucketHotDir, nextPartNumber, _partSizeMb);
        if (!_partsMap.TryAdd(nextPartNumber, partStorage))
            throw new InvalidOperationException($"Duplicate part number {nextPartNumber}");
        return partStorage;
    }
}