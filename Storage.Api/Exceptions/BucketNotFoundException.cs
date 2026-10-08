namespace Storage.Api.Exceptions;

internal sealed class BucketNotFoundException(string bucketId)
    : ResourceNotFoundException($"Корзина {bucketId} не найдена.");