using System.Collections.Concurrent;
using System.Diagnostics;
using Storage.Api.Internal;
using Storage.Api.Lss.Model;

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
        LoadParts(_bucketHotDir);
        LoadParts(_bucketColdDir);
        _partStorage ??= AddActivePart();
    }

    public async Task<DataLocation> Write(FileHeader fileHeader, Stream data, CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Запись файла {fileHeader.FileName} в корзину {_bucketName}");

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

    public async Task Read(DataLocation location, Action<FileHeader> headerCallback, Stream outStream,
        CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Чтение файла по смещению {location.Offset} из корзины {_bucketName}");

        if (_partsMap.TryGetValue(location.PartNumber, out var part))
            await part.Read(location.Offset, outStream, headerCallback, token);
        else
            throw new InvalidOperationException($"Part {location.PartNumber} not found");
    }

    public async Task DeleteAll(CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Удаление корзины {_bucketName} вместе с файлами.");

        foreach (var part in _partsMap.Values)
            await part.Delete(token);
        _partsMap.Clear();
        if (Directory.Exists(_bucketHotDir))
            Directory.Delete(_bucketHotDir, true);
    }

    public async Task ApplyRetentionPolicy(RetentionPolicy policy, CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Применение политики хранения для корзины {_bucketName}");

        var parts = _partsMap.Values.ToList();
        foreach (var part in parts)
        {
            var partType = await part.ApplyRetentionPolicy(policy, _bucketColdDir, token);
            if (partType == PartTypeEnum.Deleted)
                _partsMap.Remove(part.PartNumber, out var p);
        }
    }

    public void Dispose()
    {
        foreach (var part in _partsMap.Values)
            part.Dispose();
    }

    private void LoadParts(string bucketDir)
    {
        using var bucketActivity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Загрузка файлов корзины {_bucketName}");

        try
        {
            if (!Directory.Exists(bucketDir))
            {
                bucketActivity?.AddEvent($"Создание директории {bucketDir} для корзины {_bucketName}");
                Directory.CreateDirectory(bucketDir);
                return;
            }

            var partFiles = Directory
                .EnumerateFiles(bucketDir, "*.lss", SearchOption.AllDirectories);
            foreach (var partFile in partFiles)
            {
                using var partActivity = StorageTelemetry.Activity.StartActivity()
                    ?.WithDisplayName($"Загрузка файла раздела {partFile} для корзины {_bucketName}");
                try
                {
                    var part = PartStorage.Create(partFile);
                    if (!_partsMap.TryAdd(part.PartNumber, part))
                        throw new InvalidOperationException(
                            $"Обнаружен дубликат раздела {part.PartNumber} для корзины {_bucketName}");
                    if (!part.IsHot)
                        continue;
                    if (_partStorage != null)
                        throw new InvalidOperationException(
                            $"Горячий раздел для корзины {_bucketName} уже существует: {_partStorage.PartNumber}, дубликат: {part.PartNumber}");
                    _partStorage = part;
                }
                catch (Exception ex2)
                {
                    partActivity?.SetError(ex2);
                    throw;
                }
            }
        }
        catch (Exception ex1)
        {
            bucketActivity?.SetError(ex1);
            throw;
        }
    }

    private PartStorage AddActivePart()
    {
        var nextPartNumber = _partsMap.Keys.Count > 0
            ? _partsMap.Keys.Max() + 1
            : 0;
        var partStorage = PartStorage.Create(_bucketHotDir, nextPartNumber, _partSizeMb);
        if (!_partsMap.TryAdd(nextPartNumber, partStorage))
            throw new InvalidOperationException($"Duplicate part number {nextPartNumber}");
        return partStorage;
    }
}