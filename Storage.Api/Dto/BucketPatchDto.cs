namespace Storage.Api.Dto;

internal sealed record BucketPatchDto(
    string? NodeId,
    TimeSpan? TimeToLive);