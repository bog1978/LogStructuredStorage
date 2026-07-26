using MinimalApi.Hosting.Exceptions;

namespace Storage.Api.Exceptions;

internal class BucketNotFoundException(int bucketId)
    : ResourceNotFoundException($"Бакет {bucketId} не найден.");