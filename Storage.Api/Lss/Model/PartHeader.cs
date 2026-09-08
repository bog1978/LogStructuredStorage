namespace Storage.Api.Lss.Model;

internal record PartHeader(
    int PartNumber,
    long WritePosition,
    PartTypeEnum PartType,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime);