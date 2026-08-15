namespace Storage.Api.Dto;

internal sealed record BucketDto(
    string BucketId,
    string NodeId,
    TimeSpan TimeToLive);