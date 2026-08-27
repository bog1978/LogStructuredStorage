namespace Storage.Api.Lss;

internal record PartHeader(
    int PartNumber,
    long WritePosition,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime);