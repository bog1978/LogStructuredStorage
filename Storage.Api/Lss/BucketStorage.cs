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

    public async Task<DataLocation> Write(FileHeader fileHeader, Stream data, CancellationToken token)
    {
        var offset = await _partStorage.TryWrite(fileHeader, data, token);
        if (offset >= 0)
            return new(_bucketName, _partStorage.PartNumber, offset);

        _partStorage.Close();
        _partStorage = AddActivePart();

        offset = await _partStorage.TryWrite(fileHeader, data, token);
        return offset >= 0
            ? new(_bucketName, _partStorage.PartNumber, offset)
            : throw new InvalidOperationException("Failed to write data");
    }

    public string Name => _bucketName;

    public async Task Read(DataLocation location, Action<FileHeader> headerCallback, Stream outStream, CancellationToken token)
    {
        if (_partsMap.TryGetValue(location.PartNumber, out var part))
            await part.Read(location.Offset, outStream, headerCallback, token);
        else
            throw new InvalidOperationException($"Part {location.PartNumber} not found");
    }

    public async Task DeleteAll(CancellationToken token)
    {
        foreach (var part in _partsMap.Values)
            await part.Delete(token);
        _partsMap.Clear();
        if (Directory.Exists(_bucketHotDir))
            Directory.Delete(_bucketHotDir, true);
    }

    public void Dispose()
    {
        foreach (var part in _partsMap.Values)
            part.Dispose();
    }

    public async Task ApplyRetentionPolicy(RetentionPolicy policy, CancellationToken token)
    {
        var parts = _partsMap.Values.ToList();
        foreach (var part in parts)
        {
            if (part.CanWrite)
                continue;
            // Полное время жизни складывается из горячего и холодного.
            if (part.MaxTime + policy.TtlHot + policy.TtlCold < DateTimeOffset.UtcNow)
            {
                await part.Delete(token);
                _partsMap.Remove(part.PartNumber, out var p);
            }
            else if (part.IsHot && part.MaxTime + policy.TtlHot < DateTimeOffset.UtcNow)
            {
                await part.MakeCold(_bucketColdDir, token);
            }
            else
            {
                // Пускай еще побудет тепленьким.
            }
        }
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