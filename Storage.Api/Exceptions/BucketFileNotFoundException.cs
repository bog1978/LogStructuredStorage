using MinimalApi.Hosting.Exceptions;

namespace Storage.Api.Exceptions;

internal sealed class BucketFileNotFoundException(string bucketId, string filePath)
    : ResourceNotFoundException($"В корзине {bucketId} не найден файл {filePath}.");