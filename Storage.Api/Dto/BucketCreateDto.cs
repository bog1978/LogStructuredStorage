namespace Storage.Api.Dto;

internal record BucketCreateDto(
    string BucketId,
    string NodeId,
    TimeSpan TimeToLive);