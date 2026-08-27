namespace Storage.Cluster.Exceptions;

public sealed class BucketNotFoundException(string bucketId)
    : ResourceNotFoundException($"Бакет {bucketId} не найден.");