namespace Storage.Api.Lss;

internal record PartHeader(
    int PartNumber,
    long WritePosition,
    PartTypeEnum PartType,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime);