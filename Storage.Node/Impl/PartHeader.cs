namespace Storage.Node.Impl;

internal record PartHeader(
    int PartNumber,
    long WritePosition,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime);