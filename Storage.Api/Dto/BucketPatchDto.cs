namespace Storage.Api.Dto;

internal record BucketPatchDto(
    string? NodeId,
    TimeSpan? TimeToLive);