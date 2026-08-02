using MinimalApi.Hosting.Exceptions;

namespace Storage.Api.Exceptions;

internal class BucketNotFoundException(string bucketId)
    : ResourceNotFoundException($"Бакет {bucketId} не найден.");