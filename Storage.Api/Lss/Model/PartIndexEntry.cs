namespace Storage.Api.Lss.Model;

internal readonly record struct PartIndexEntry(
    long RecordOffset,
    long RecordLength,
    int FileLength,
    DateTimeOffset CreatedAt);
