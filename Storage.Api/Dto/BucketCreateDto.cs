namespace Storage.Api.Dto;

internal record BucketCreateDto(
    string BucketName,
    int NodeId,
    TimeSpan TimeToLive);