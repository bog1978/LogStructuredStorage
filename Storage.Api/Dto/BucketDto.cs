namespace Storage.Api.Dto;

internal record BucketDto(
    int BucketId,
    string BucketName,
    int NodeId,
    TimeSpan TimeToLive);