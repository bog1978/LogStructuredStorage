using System.Collections.Concurrent;
using Storage.Api.Internal;
using Storage.Api.Lss.Model;

namespace Storage.Api.Lss;

/// <summary>
/// Хранит данные корзины в виде набора файлов-разделов (.lss). Каждый раздел может находиться
/// в одном из состояний: Hot (чтение + запись), Warm (только чтение), Cold (чтение + удаление).
/// Синхронизирует одновременный доступ к нескольким разделам из нескольких потоков.
/// </summary>
internal sealed class BucketStorage : IBucketStorage
{
    private const int WriteRetryCount = 10;
    private readonly string _bucketName;
    private readonly int _partSizeMb;
    private readonly string _bucketHotDir;
    private readonly string _bucketColdDir;
    private readonly ConcurrentDictionary<int, PartStorage> _partsMap = new();
    private volatile PartStorage? _partStorage;
    private readonly Lock _lock = new();

    public BucketStorage(string hotDir, string coldDir, string bucketName, int partSizeMb)
    {
        _bucketName = bucketName;
        _partSizeMb = partSizeMb;
        _bucketHotDir = Path.Combine(hotDir, bucketName);
        _bucketColdDir = Path.Combine(coldDir, bucketName);
        LoadParts(_bucketHotDir);
        LoadParts(_bucketColdDir);
        _partStorage = AddActivePart();
    }

    public string Name => _bucketName;

    public async Task<DataLocation> Write(FileHeader fileHeader, Stream data, CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Запись файла {fileHeader.FileName} в корзину {_bucketName}");

        // Запоминаем раздел, с которым работаем.
        PartStorage hotPart;
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_partStorage == null, this);
            hotPart = _partStorage;
        }

        // Если другой поток успел создать новый горячий раздел и заполнить его,
        // то нужно будет делать повторные попытки. Но это не штатная ситуация,
        // т.к. файлы должны быть маленькие, а разделы - большие. Даже если идет
        // запись в несколько потоков, то они все равно не успеют заполнить раздел
        // между созданием нового горячего раздела и записью в него.
        // Этот цикл - перестраховка на всякий случай.
        for (var i = 0; i < WriteRetryCount; i++)
        {
            var offset = await hotPart.TryWrite(fileHeader, data, token);
            if (offset >= 0)
                return new(_bucketName, hotPart.PartNumber, offset);
            
            lock (_lock)
                if(ReferenceEquals(_partStorage, hotPart))
                {
                    // Создаем новый горячий раздел. Это нормальная ситуация: начали писать
                    // в горячий раздел, но он заполнился и создали новый, чтобы продолжить писать.
                    hotPart = AddActivePart();
                    _partStorage = hotPart;
                }
                else
                {
                    // В результате гонок другой поток уже мог создать новый горячий раздел.
                    // Будем писать в тот, который уже есть.
                    hotPart = _partStorage;
                }
        }
        
        // В этом месте новый горячий раздел создан, но записи в него не было.
        // Ничего страшного в этом нет, т.к. при следующей записи он все равно создался бы.
        throw new InvalidOperationException("Failed to write data");
    }

    public async Task Read(DataLocation location, Action<FileHeader> headerCallback, Stream outStream,
        CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Чтение файла по смещению {location.Offset} из корзины {_bucketName}");

        lock (_lock)
            ObjectDisposedException.ThrowIf(_partStorage == null, this);

        if (_partsMap.TryGetValue(location.PartNumber, out var part))
            await part.Read(location.Offset, outStream, headerCallback, token);
        else
            throw new InvalidOperationException($"Part {location.PartNumber} not found");
    }

    /// <summary>
    /// Удаляет все файлы корзины вместе с разделами, но не удаляет саму корзину.
    /// </summary>
    /// <param name="token"></param>
    public async Task DeleteAll(CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Удаление корзины {_bucketName} вместе с файлами.");

        List<PartStorage> parts;
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_partStorage == null, this);
            parts = _partsMap.Values.ToList();
            _partsMap.Clear();
            _partStorage = AddActivePart();
        }

        foreach (var part in parts)
            await part.Delete(token);
    }

    /// <summary>
    /// Применяет политику хранения к разделам корзины:
    /// 1. Перемещает тёплые файлы в холодные.
    /// 2. Удаляет холодные файлы.
    /// </summary>
    /// <param name="policy"></param>
    /// <param name="token"></param>
    public async Task ApplyRetentionPolicy(RetentionPolicy policy, CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Применение политики хранения для корзины {_bucketName}");

        lock (_lock)
            ObjectDisposedException.ThrowIf(_partStorage == null, this);

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
        lock (_lock)
        {
            foreach (var part in _partsMap.Values)
                part.Dispose();
            _partsMap.Clear();
            _partStorage = null;
        }
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