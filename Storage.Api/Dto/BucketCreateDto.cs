namespace Storage.Api.Dto;

internal sealed record BucketCreateDto(
    string BucketId,
    string NodeId,
    TimeSpan TtlHot,
    TimeSpan TtlCold);