namespace Storage.Api.Exceptions;

public sealed class BucketNotFoundException(string bucketId)
    : ResourceNotFoundException($"Бакет {bucketId} не найден.");