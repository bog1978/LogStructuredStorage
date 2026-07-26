namespace Storage.Api.Dto;

internal record BucketPatchDto(
    string? BucketName,
    int? NodeId,
    TimeSpan? TimeToLive);