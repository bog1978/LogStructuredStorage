namespace Storage.Node;

public sealed record DataLocation(
    int PartNumber,
    long Offset);