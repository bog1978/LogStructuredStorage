namespace Storage.Node;

internal record PartHeader(
    int PartNumber,
    long WritePosition,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime);