namespace Storage.Api.Services;

internal record PartHeader(
    int PartNumber,
    long WritePosition,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime);