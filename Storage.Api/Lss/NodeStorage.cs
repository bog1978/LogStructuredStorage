using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Storage.Api.Internal;
using Storage.Api.Lss.Model;
using Storage.Api.Options;

namespace Storage.Api.Lss;

internal sealed class NodeStorage : INodeStorage
{
    private readonly StorageOptions _options;
    private bool _disposed;

    private readonly ConcurrentDictionary<string, BucketStorage> _bucketMap = new();

    public NodeStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        LoadBuckets();
    }

    public IBucketStorage GetBucket(string bucketName) =>
        _bucketMap.TryGetValue(bucketName, out var bucketStorage)
            ? bucketStorage
            : throw new InvalidOperationException("Корзина не найдена");

    public IBucketStorage GetOrCreateBucket(string bucketName) =>
        _bucketMap.GetOrAdd(bucketName, key => new(
            _options.HotPath,
            _options.ColdPath,
            key,
            _options.PartSizeMb));

    public async Task DeleteAll(CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Удаление всех данных узла {_options.HotPath}");

        foreach (var bucketStorage in _bucketMap.Values)
            await bucketStorage.DeleteAll(token);
        _bucketMap.Clear();
        activity?.AddEvent($"Удалены данные всех корзин");
    }

    public async Task ApplyRetentionPolicy(Func<string, RetentionPolicy> policyFunc, CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Применение политики хранения для узла {_options.HotPath}");

        var count = 0;
        foreach (var bucketStorage in _bucketMap.Values)
        {
            await bucketStorage.ApplyRetentionPolicy(policyFunc(bucketStorage.Name), token);
            count++;
        }
        activity?.AddEvent($"Политика применена ко всем {count} корзинам");
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var bucketStorage in _bucketMap.Values)
            bucketStorage.Dispose();
    }

    private void LoadBuckets()
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Загрузка корзин с узла {_options.HotPath}");

        if (!Directory.Exists(_options.HotPath))
            Directory.CreateDirectory(_options.HotPath);
        var bucketDirs = Directory.EnumerateDirectories(_options.HotPath);
        var count = 0;
        foreach (var bucketDir in bucketDirs)
        {
            var bucketName = Path.GetFileName(bucketDir);
            var bucketStorage = new BucketStorage(
                _options.HotPath,
                _options.ColdPath,
                bucketName,
                _options.PartSizeMb);
            if (!_bucketMap.TryAdd(bucketName, bucketStorage))
                throw new InvalidOperationException($"Корзина {bucketName} уже существует");
            count++;
        }
        activity?.AddEvent($"Загружено {count} корзин");
    }
}