namespace Storage.Api.Dto;

internal record BucketDto(
    string BucketId,
    string NodeId,
    TimeSpan TimeToLive);