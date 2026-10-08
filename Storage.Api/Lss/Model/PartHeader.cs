namespace Storage.Api.Lss.Model;

internal record PartHeader(
    uint Magic,
    byte Version,
    int PartNumber,
    PartTypeEnum PartType,
    DateTimeOffset CreatedAt,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime,
    int WritePosition,
    int CommittedFileCount);
