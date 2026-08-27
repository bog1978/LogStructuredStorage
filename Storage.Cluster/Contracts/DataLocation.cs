namespace Storage.Cluster;

public sealed record DataLocation(
    string BucketName,
    int PartNumber,
    long Offset);