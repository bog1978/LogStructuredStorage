namespace Storage.Api.Exceptions;

public sealed class BucketFileNotFoundException(string bucketId, string filePath)
    : ResourceNotFoundException($"В корзине {bucketId} не найден файл {filePath}.");